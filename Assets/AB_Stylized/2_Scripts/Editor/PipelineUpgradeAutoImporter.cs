using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnkleBreaker.Tools
{
    /// <summary>
    /// AnkleBreaker packages ship their materials for the Built-in pipeline, with URP and HDRP
    /// versions sitting in 3_UpgradePackages/. The Asset Store cannot pick one for you: a listing
    /// serves the same file to every buyer, whatever their render pipeline.
    ///
    /// The offer is the pack's Welcome window, which opens on its own the first time: its pipeline
    /// band counts the materials built for another pipeline and its Convert button runs the menu
    /// below. No dialog pops up at import: a modal on arrival is the worst place to present the
    /// pack. Batch mode still applies the upgrade by itself, since no one is there to click,
    /// unless -abNoAutoUpgrade is on the command line.
    ///
    /// Scope, deliberately narrow: this only ever imports a .unitypackage that shipped inside
    /// AB_Stylized/3_UpgradePackages/, and an import can only write the paths that archive
    /// contains. Nothing outside those files is read or written. The archives are opened
    /// beforehand so the log NAMES the files it replaces.
    ///
    /// No reference to the URP or HDRP assemblies: the active pipeline is read from
    /// GraphicsSettings and identified by type name, so this file compiles in a project that has
    /// neither installed - which is most of them.
    /// </summary>
    public class PipelineUpgradeAutoImporter : AssetPostprocessor
    {
        private const string Root = "Assets/AB_Stylized";
        private const string UpgradeRoot = Root + "/3_UpgradePackages";
        private const string AppliedThisSessionKey = "AnkleBreaker.PipelineUpgrade.Asked";
        private const string MenuPath = "Tools/AnkleBreaker/Apply Render Pipeline Upgrade";
        private const string OptOutArg = "-abNoAutoUpgrade";
        private const int MaxListed = 12;

        private static Queue<string> _pending;

        /// <summary>
        /// An upgrade import is in flight. Outside batch mode AssetDatabase.ImportPackage is
        /// ASYNCHRONOUS: Apply returns before a single .mat has been replaced, so a second click
        /// meanwhile would re-read the untouched files on disk, find the same materials to fix,
        /// and queue the very same upgrade again.
        /// </summary>
        private static bool _busy;

        /// <summary>A batch import (CI, a build farm) should not silently rewrite assets.</summary>
        private static bool OptedOut =>
            Environment.GetCommandLineArgs().Any(a => string.Equals(a, OptOutArg, StringComparison.Ordinal));

        /// <summary>An upgrade package, and the assets it would actually overwrite.</summary>
        private class Overlay
        {
            public string PackagePath;
            public List<string> Payload = new List<string>();
            public List<string> MaterialsToFix = new List<string>();

            /// <summary>
            /// Already right on disk, still wrong in the AssetDatabase: the file was replaced but
            /// never reimported (see FreshCopy). Importing the overlay again would change nothing
            /// on disk; these need a reimport instead.
            /// </summary>
            public List<string> StaleImports = new List<string>();

            public string Name => Path.GetFileName(PackagePath);
        }

        /// <summary>
        /// Batch only. This is the entry point that fires the first time a package is imported:
        /// this very file arrives WITH the package, so the assembly holding it does not exist yet
        /// when the import callback runs; the domain reload that follows the compilation is the
        /// first chance. There is no guarantee a delayed call runs before a batch editor quits,
        /// so Apply runs right away.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CheckOnLoad()
        {
            if (!Application.isBatchMode || OptedOut || SessionState.GetBool(AppliedThisSessionKey, false))
                return;

            SessionState.SetBool(AppliedThisSessionKey, true);
            Apply(false);
        }

        /// <summary>Batch only: an overlay that lands once the assembly exists (a second pack).</summary>
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFromAssetPaths)
        {
            if (!Application.isBatchMode || OptedOut) return;

            var arrived = imported.Any(p =>
                p.StartsWith(UpgradeRoot, StringComparison.OrdinalIgnoreCase) &&
                p.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase));
            if (!arrived)
                return;

            SessionState.SetBool(AppliedThisSessionKey, true);
            Apply(false);
        }

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            Apply(true);
        }

        private static void Apply(bool fromMenu)
        {
            if (_busy)
            {
                // Not an error: the import simply has not landed yet.
                if (fromMenu)
                    EditorUtility.DisplayDialog("AnkleBreaker",
                        "A render pipeline upgrade is being imported right now.\n\n" +
                        "Wait for it to finish, then run this menu item again if anything is left.", "OK");
                return;
            }

            var pipeline = ActivePipeline();
            if (pipeline == null)
            {
                if (fromMenu)
                    EditorUtility.DisplayDialog("AnkleBreaker",
                        "The active render pipeline could not be identified, so no upgrade package can be chosen.\n\n" +
                        $"Import the matching .unitypackage from {UpgradeRoot} by hand.", "OK");
                return;
            }

            var overlays = Overlays(pipeline);

            // Only offer packages that would actually change something. Scanning AB_Stylized for
            // mismatched materials is not enough on its own: a pack with no upgrade package of its
            // own (an older Built-in pack, say) would be counted and implicitly promised a fix that
            // importing someone else's overlay will never deliver.
            var actionable = overlays.Where(o => o.MaterialsToFix.Count > 0).ToList();

            // A project left half-converted (right files, stale imports) is repaired here, not
            // told "nothing to import" while the Welcome band still counts magenta materials.
            var stale = overlays.SelectMany(o => o.StaleImports).ToList();
            if (stale.Count > 0)
            {
                Debug.Log($"[AnkleBreaker] {stale.Count} material(s) already hold their {pipeline} version on disk " +
                          "but were never reimported. Reimporting them:\n    " + string.Join("\n    ", stale));
                Reimport(stale);
            }

            if (actionable.Count == 0)
            {
                if (stale.Count > 0)
                {
                    if (fromMenu)
                        EditorUtility.DisplayDialog("AnkleBreaker",
                            $"{stale.Count} material(s) under {Root} already had their {pipeline} version on disk " +
                            "but Unity had not reimported them. They have been reimported now.", "OK");
                    return;
                }

                if (fromMenu)
                    EditorUtility.DisplayDialog("AnkleBreaker",
                        $"This project renders with {pipeline}, and every AnkleBreaker material under\n" +
                        $"{Root}\nthat has a matching upgrade package is already set up for it.\n\n" +
                        (overlays.Count == 0
                            ? $"No {pipeline} upgrade package was found under {UpgradeRoot}."
                            : "Nothing to import."), "OK");
                return;
            }

            var count = actionable.Sum(o => o.MaterialsToFix.Count);

            Debug.Log($"[AnkleBreaker] {pipeline} detected. Importing {actionable.Count} upgrade package(s), " +
                      $"which replace {count} material(s) under {Root} and nothing else:\n    " +
                      string.Join("\n    ", actionable.SelectMany(o => o.MaterialsToFix)));

            Import(actionable.Select(o => o.PackagePath).ToList(), pipeline,
                   actionable.SelectMany(o => o.MaterialsToFix).ToList());
        }

        /// <summary>
        /// What the menu would import in this project right now, named file by file. Public so it
        /// can be read and checked without a GUI.
        /// </summary>
        public static string PreviewMessage()
        {
            var pipeline = ActivePipeline();
            if (pipeline == null) return "(active render pipeline not identified)";
            var actionable = Overlays(pipeline).Where(o => o.MaterialsToFix.Count > 0).ToList();
            if (actionable.Count == 0) return $"(nothing to offer: pipeline {pipeline})";
            return BuildMessage(pipeline, actionable, actionable.Sum(o => o.MaterialsToFix.Count));
        }

        private static string BuildMessage(string pipeline, List<Overlay> actionable, int count)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"This project renders with {pipeline}.");
            sb.AppendLine();
            sb.AppendLine(count == 1
                ? $"One material under {Root} is built for another pipeline, and the pack ships a ready-made {pipeline} version of it:"
                : $"{count} materials under {Root} are built for another pipeline, and their packs ship ready-made {pipeline} versions:");
            sb.AppendLine();

            var listed = 0;
            foreach (var overlay in actionable)
            {
                sb.AppendLine("    " + overlay.Name);
                foreach (var path in overlay.MaterialsToFix)
                {
                    if (listed++ >= MaxListed) continue;
                    sb.AppendLine("        " + path);
                }
            }
            if (listed > MaxListed)
                sb.AppendLine($"        ... and {listed - MaxListed} more");

            sb.AppendLine();
            sb.AppendLine($"{MenuPath} imports them.");
            sb.AppendLine();
            sb.AppendLine("Only the files listed above are replaced. They overwrite the AnkleBreaker materials");
            sb.AppendLine("in place, so your prefabs and scenes keep working. Nothing else in your project is");
            sb.AppendLine("read or written.");
            return sb.ToString();
        }

        private static List<string> _touched;

        private static void Import(List<string> packagePaths, string pipeline, List<string> touched)
        {
            _pending = new Queue<string>(packagePaths);
            _touched = touched;

            if (Application.isBatchMode)
            {
                // Batch has no one to wait for the asynchronous import to finish, and the editor
                // may quit first. ImportPackageImmediately is internal, so it is reached by
                // reflection - and if a future Unity drops it, we fall back to the public call
                // rather than silently importing nothing.
                var immediate = typeof(AssetDatabase).GetMethod("ImportPackageImmediately",
                    BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
                while (_pending.Count > 0)
                {
                    var path = FreshCopy(_pending.Dequeue());
                    if (immediate != null) immediate.Invoke(null, new object[] { path });
                    else AssetDatabase.ImportPackage(path, false);
                }
                AssetDatabase.Refresh();
                Report(pipeline, touched);
                return;
            }

            _busy = true;

            AssetDatabase.importPackageCompleted -= OnOneDone;
            AssetDatabase.importPackageCompleted += OnOneDone;
            AssetDatabase.importPackageFailed -= OnOneFailed;
            AssetDatabase.importPackageFailed += OnOneFailed;
            AssetDatabase.importPackageCancelled -= OnOneCancelled;
            AssetDatabase.importPackageCancelled += OnOneCancelled;
            Next(pipeline);
        }

        private static void Next(string pipeline)
        {
            if (_pending == null || _pending.Count == 0)
            {
                _busy = false;
                AssetDatabase.importPackageCompleted -= OnOneDone;
                AssetDatabase.importPackageFailed -= OnOneFailed;
                AssetDatabase.importPackageCancelled -= OnOneCancelled;
                Report(pipeline, _touched);
                return;
            }

            AssetDatabase.ImportPackage(FreshCopy(_pending.Dequeue()), false);
        }

        /// <summary>
        /// Unity writes each file of a .unitypackage with the modification time stored in the
        /// archive. An overlay built BEFORE the pack that carries it therefore writes its materials
        /// with a date older than the one the AssetDatabase recorded for them; Unity 6 then refuses
        /// to import them ("Build asset version error ... modification time"), retries in a loop,
        /// and the materials stay magenta although the right file is on disk. Measured on
        /// EnvironmentRocks, 30/09/2026: overlay of 27/09 inside a base of 30/09.
        ///
        /// So the overlay is never imported as is: a copy under Temp/ gets every entry dated now.
        /// Only the tar headers change (mtime and checksum); names and contents are copied byte
        /// for byte. If the copy cannot be written, the original is imported, as before.
        /// </summary>
        private static string FreshCopy(string packagePath)
        {
            try
            {
                var folder = Path.Combine("Temp", "AnkleBreakerPipelineUpgrade");
                Directory.CreateDirectory(folder);
                var copy = Path.Combine(folder, Path.GetFileName(packagePath)).Replace('\\', '/');
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                using (var input = new GZipStream(File.OpenRead(packagePath), CompressionMode.Decompress))
                using (var output = new GZipStream(File.Create(copy), System.IO.Compression.CompressionLevel.Optimal))
                {
                    var header = new byte[512];
                    while (ReadExactly(input, header, 512))
                    {
                        if (header.All(b => b == 0))
                        {
                            output.Write(header, 0, 512);
                            continue;
                        }

                        var size = Octal(header, 124, 12);
                        Restamp(header, now);
                        output.Write(header, 0, 512);

                        var remaining = (size + 511) / 512 * 512;
                        var buffer = new byte[81920];
                        while (remaining > 0)
                        {
                            var n = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                            if (n <= 0) throw new EndOfStreamException("Truncated archive: " + packagePath);
                            output.Write(buffer, 0, n);
                            remaining -= n;
                        }
                    }
                }
                return copy;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnkleBreaker] Could not prepare a fresh copy of \"{Path.GetFileName(packagePath)}\" " +
                                 $"({e.Message}); importing the original.");
                return packagePath;
            }
        }

        /// <summary>Sets a tar header's mtime and recomputes its checksum (POSIX ustar layout).</summary>
        private static void Restamp(byte[] header, long unixSeconds)
        {
            var mtime = Encoding.ASCII.GetBytes(Convert.ToString(unixSeconds, 8).PadLeft(11, '0') + "\0");
            Array.Copy(mtime, 0, header, 136, 12);

            for (var i = 148; i < 156; i++) header[i] = (byte)' ';
            long sum = 0;
            foreach (var b in header) sum += b;
            var checksum = Encoding.ASCII.GetBytes(Convert.ToString(sum, 8).PadLeft(6, '0') + "\0 ");
            Array.Copy(checksum, 0, header, 148, 8);
        }

        private static long Octal(byte[] header, int offset, int length)
        {
            long value = 0;
            foreach (var c in Encoding.ASCII.GetString(header, offset, length).Trim('\0', ' '))
            {
                if (c < '0' || c > '7') return 0;
                value = value * 8 + (c - '0');
            }
            return value;
        }

        /// <summary>
        /// Reimports files that are already right on disk. Their date moves to now first: an
        /// AssetDatabase stuck on an older record would otherwise keep rejecting them.
        /// </summary>
        private static void Reimport(List<string> paths)
        {
            var now = DateTime.UtcNow;
            foreach (var path in paths)
            {
                if (File.Exists(path)) File.SetLastWriteTimeUtc(path, now);
                if (File.Exists(path + ".meta")) File.SetLastWriteTimeUtc(path + ".meta", now);
            }

            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void OnOneDone(string packageName) => Next(ActivePipeline());

        private static void OnOneFailed(string packageName, string error)
        {
            Debug.LogError($"[AnkleBreaker] Upgrade package \"{packageName}\" failed to import: {error}");
            Next(ActivePipeline());
        }

        private static void OnOneCancelled(string packageName)
        {
            Debug.LogWarning($"[AnkleBreaker] Upgrade package \"{packageName}\" was cancelled.");
            _pending = null;
            Next(ActivePipeline());
        }

        /// <summary>
        /// On dit ce qu'on a fait, on ne le "verifie" pas dans la foulee : les assets qu'on vient
        /// d'ecraser sont encore charges en memoire, et un recomptage immediat remonte un
        /// avertissement APRES UN IMPORT REUSSI - mesure. Le vrai filet est la bande pipeline de
        /// la Welcome, qui recompte a la fin de l'import et reste rouge s'il reste un materiau.
        /// </summary>
        private static void Report(string pipeline, List<string> touched)
        {
            var count = touched == null ? 0 : touched.Count;
            Debug.Log($"[AnkleBreaker] {count} material(s) replaced with their {pipeline} version. " +
                      $"You can delete {UpgradeRoot} if you no longer need it.");
        }

        private static string ActivePipeline()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null) return "Built-in";
            var name = rp.GetType().Name;
            if (name.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) >= 0) return "URP";
            if (name.IndexOf("HDRenderPipeline", StringComparison.OrdinalIgnoreCase) >= 0) return "HDRP";
            return null;
        }

        private static List<Overlay> Overlays(string pipeline)
        {
            var result = new List<Overlay>();
            if (pipeline == "Built-in" || !AssetDatabase.IsValidFolder(UpgradeRoot))
                return result;

            var suffix = "_" + pipeline + ".unitypackage";
            var files = Directory.GetFiles(UpgradeRoot, "*.unitypackage", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.Ordinal);

            foreach (var file in files)
            {
                var overlay = new Overlay { PackagePath = file };
                Dictionary<string, byte[]> entries;
                try
                {
                    entries = ReadPackageEntries(file);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AnkleBreaker] Could not read \"{Path.GetFileName(file)}\": {e.Message}");
                    continue;
                }

                overlay.Payload = entries.Keys.ToList();

                foreach (var pair in entries)
                {
                    if (!pair.Key.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) continue;
                    if (pair.Value == null || !File.Exists(pair.Key)) continue;

                    // On compare le shader que l'overlay APPORTE a celui qui est DEJA sur le
                    // disque. Passer par l'AssetDatabase ici serait un piege de timing : quand
                    // le domaine recharge au milieu d'un import, le materiau n'y est pas encore
                    // et LoadAssetAtPath rend null - l'auto-upgrade restait alors muet.
                    var incoming = ShaderGuid(Encoding.UTF8.GetString(pair.Value));
                    var present = ShaderGuid(File.ReadAllText(pair.Key));
                    if (incoming != null && present != null && incoming != present)
                        overlay.MaterialsToFix.Add(pair.Key);
                    else if (incoming != null && incoming == present && IsImportStale(pair.Key, incoming))
                        overlay.StaleImports.Add(pair.Key);
                }

                result.Add(overlay);
            }

            return result;
        }

        /// <summary>
        /// The file on disk points at the overlay's shader, but the imported material does not:
        /// the one the Welcome band counts as magenta. A material the AssetDatabase cannot load
        /// yet (mid-import) is not judged: that is the timing trap described above.
        /// </summary>
        private static bool IsImportStale(string path, string expectedShaderGuid)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) return false;
            if (material.shader == null) return true;
            return !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material.shader, out string guid, out long _)
                   || guid != expectedShaderGuid;
        }

        /// <summary>
        /// GUID du shader reference par un fichier .mat. Deux materiaux au meme chemin qui ne
        /// pointent pas le meme shader : importer l'overlay change quelque chose. C'est la seule
        /// question a laquelle il faut repondre, et elle se lit dans le fichier.
        /// </summary>
        private static string ShaderGuid(string materialYaml)
        {
            var marker = "m_Shader:";
            var at = materialYaml.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0) return null;
            var guidAt = materialYaml.IndexOf("guid: ", at, StringComparison.Ordinal);
            if (guidAt < 0 || guidAt - at > 120) return null;
            guidAt += 6;
            if (guidAt + 32 > materialYaml.Length) return null;
            return materialYaml.Substring(guidAt, 32);
        }

        /// <summary>
        /// Assets contained in a .unitypackage : chemin -> contenu (pour les petits fichiers).
        /// Le format est un tar gzippe, un dossier par asset, avec une entree "pathname" et une
        /// entree "asset". Lecture sequentielle : le flux est gzippe, il ne se seek pas.
        /// </summary>
        private static Dictionary<string, byte[]> ReadPackageEntries(string packagePath)
        {
            var byGuid = new Dictionary<string, string>();
            var blobs = new Dictionary<string, byte[]>();
            var header = new byte[512];

            using (var file = File.OpenRead(packagePath))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                while (true)
                {
                    if (!ReadExactly(gzip, header, 512)) break;
                    if (header[0] == 0) break; // bloc de fin

                    var name = Encoding.UTF8.GetString(header, 0, 100).TrimEnd('\0', ' ').Replace('\\', '/');
                    var sizeText = Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' ');
                    long size = 0;
                    foreach (var c in sizeText)
                    {
                        if (c < '0' || c > '7') { size = 0; break; }
                        size = size * 8 + (c - '0');
                    }

                    var padded = (size + 511) / 512 * 512;
                    var folder = name.Contains("/") ? name.Substring(0, name.LastIndexOf('/')) : name;
                    var leaf = name.Substring(name.LastIndexOf('/') + 1);
                    var wanted = (leaf == "pathname" || leaf == "asset") && size > 0 && size < 4 * 1024 * 1024;

                    var buffer = new byte[padded];
                    if (padded > 0 && !ReadExactly(gzip, buffer, (int)padded)) break;
                    if (!wanted) continue;

                    if (leaf == "pathname")
                    {
                        var assetPath = Encoding.UTF8.GetString(buffer, 0, (int)size)
                            .Split('\n')[0].Trim().Replace('\\', '/');
                        if (assetPath.Length > 0) byGuid[folder] = assetPath;
                    }
                    else
                    {
                        var content = new byte[size];
                        Array.Copy(buffer, content, size);
                        blobs[folder] = content;
                    }
                }
            }

            var entries = new Dictionary<string, byte[]>();
            foreach (var pair in byGuid)
                entries[pair.Value] = blobs.TryGetValue(pair.Key, out var blob) ? blob : null;
            return entries;
        }

        private static bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            var read = 0;
            while (read < count)
            {
                var n = stream.Read(buffer, read, count - read);
                if (n <= 0) return false;
                read += n;
            }
            return true;
        }
    }
}
