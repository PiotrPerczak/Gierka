using System;

// Every field below is written by JsonUtility, never by code, which the compiler reports as
// CS0649 ("never assigned"): 52 warnings in a buyer's console on a strict compiler setup.
#pragma warning disable 0649

namespace AnkleBreaker.EnvironmentRocks.Editor.Welcome
{
    // Stamped from _WelcomeWindow/Template by `welcome.py stamp`. Edit the template, not this copy:
    // a re-stamp overwrites it.

    /// <summary>
    /// Everything the window says about its package, read from the <c>*.welcome.json</c> next to
    /// this file. The C# holds no product text: a package changes its window by editing that file.
    ///
    /// <para>JsonUtility never leaves a nested object null, so an absent block reads as an empty
    /// one. Every consumer tests <c>label</c> or <c>id</c> for emptiness, not the reference.</para>
    /// </summary>
    [Serializable]
    internal sealed class EnvironmentRocksWelcomeData
    {
        public int schema = 1;

        /// <summary><c>tool</c> or <c>art</c>. Same shell, different left column.</summary>
        public string profile = "tool";

        /// <summary>Catalogue id, which is how the showcase knows not to advertise this package
        /// to itself.</summary>
        public string id;
        public string name;
        public string tagline;

        /// <summary>Empty: the version of the Package Manager package holding this Welcome, read
        /// at display time, so a Welcome shipped in a GitHub package never lags its releases.</summary>
        public string version;

        /// <summary>False when this Welcome ships in a package distributed outside the Asset Store
        /// (a GitHub package): its version is that package's, which the store product's version
        /// says nothing about, so no LATEST or IS OUT claim and no update band.</summary>
        public bool storeVersion = true;
        public int welcomeRevision = 1;
        public string icon = "Media/Icon.png";

        /// <summary>Package root, relative to the folder holding the config. Every other path in
        /// the config is relative to that root, so a buyer can move the package folder.</summary>
        public string root = "..";

        /// <summary>The package's main assembly. Absent from the loaded assemblies means the
        /// package did not compile, which is a blocking state of its own.</summary>
        public string ownAssembly;

        public EnvironmentRocksRequirement[] requires = new EnvironmentRocksRequirement[0];

        /// <summary>Pipelines the package renders with: <c>builtin</c>, <c>urp</c>, <c>hdrp</c>.
        /// Empty means it does not care.</summary>
        public string[] pipelines = new string[0];

        public EnvironmentRocksAction hero = new EnvironmentRocksAction();
        public EnvironmentRocksAction heroAlt = new EnvironmentRocksAction();
        public EnvironmentRocksStep[] steps = new EnvironmentRocksStep[0];

        /// <summary>The OPEN grid that replaces the steps once they are all done.</summary>
        public EnvironmentRocksAction[] shortcuts = new EnvironmentRocksAction[0];

        public EnvironmentRocksStat[] stats = new EnvironmentRocksStat[0];
        public string statsTitle = "INSIDE THE BOX";

        public EnvironmentRocksHelp help = new EnvironmentRocksHelp();
        public EnvironmentRocksUsage usage = new EnvironmentRocksUsage();

        public EnvironmentRocksBoard board = new EnvironmentRocksBoard();
        public EnvironmentRocksTie tie = new EnvironmentRocksTie();
        public EnvironmentRocksAction[] utilities = new EnvironmentRocksAction[0];
        public EnvironmentRocksPipeline pipelineBand = new EnvironmentRocksPipeline();

        public EnvironmentRocksShowcase showcase = new EnvironmentRocksShowcase();
        public bool recommendationsOnly;
        public bool studioTab = true;
        public EnvironmentRocksStudio studio = new EnvironmentRocksStudio();
    }

    [Serializable]
    internal sealed class EnvironmentRocksRequirement
    {
        public string name;

        /// <summary>What <c>Client.AddAndRemove</c> installs: a Unity registry name or a Git URL.
        /// Empty for a package only the Asset Store sells, which <c>url</c> then points to.</summary>
        public string packageId;
        public string assembly;
        public string url;

        /// <summary>What the package loses without it, shown while it is missing. Only read for
        /// a requirement the package compiles without.</summary>
        public string why;

        /// <summary>Needed for the package to compile, as opposed to needed by some features.</summary>
        public bool compile = true;
    }

    /// <summary>
    /// One button. <c>kind</c> is <c>scene</c> (target = scene name), <c>menu</c> (menu path),
    /// <c>url</c>, <c>ping</c> (asset path relative to the root), <c>tab</c> (<c>assets</c> or
    /// <c>studio</c>) or <c>product</c> (catalogue id).
    ///
    /// <para>Menu paths are how this window reaches the package without referencing it: it lives
    /// in an assembly that must compile when the package does not.</para>
    /// </summary>
    [Serializable]
    internal sealed class EnvironmentRocksAction
    {
        public string label;
        public string detail;
        public string kind;
        public string target;
    }

    [Serializable]
    internal sealed class EnvironmentRocksStep
    {
        public string id;
        public string title;
        public string body;
        public string doneVerb = "Done";
        public EnvironmentRocksAction action = new EnvironmentRocksAction();
    }

    /// <summary>
    /// A figure in INSIDE THE BOX. Read off the project rather than typed in whenever it can be:
    /// <c>count</c> is an AssetDatabase filter under the root (optionally <c>path</c> below it,
    /// minus paths containing <c>exclude</c>), <c>tris</c> averages the triangle count of the
    /// named prefabs. <c>value</c> is the literal fallback.
    /// </summary>
    [Serializable]
    internal sealed class EnvironmentRocksStat
    {
        public string label;
        public string value;
        public string count;
        public string path;
        public string exclude;
        public string[] guids = new string[0];
        public string[] tris = new string[0];
    }

    [Serializable]
    internal sealed class EnvironmentRocksHelp
    {
        public string note = "Someone answers, usually the same day";
        public EnvironmentRocksAction docs = new EnvironmentRocksAction();
        public string bugUrl;
        public string featureUrl;
    }

    /// <summary>
    /// What the review prompt counts. <c>filter</c> runs over the whole project minus the
    /// package root, so it counts what the buyer made with the package, not what shipped in it.
    /// </summary>
    [Serializable]
    internal sealed class EnvironmentRocksUsage
    {
        public string filter;
        public string phrase;
        public int minCount = 1;
        public string reviewUrl;
    }

    /// <summary>The prefab board of an art pack: its real inventory icons, one click per prefab.</summary>
    [Serializable]
    internal sealed class EnvironmentRocksBoard
    {
        public string title = "WHAT IS IN THE PACK";
        public string hint = "Click one to drop the prefab into the open scene";
        public string prefabs;
        public string[] prefabGuids = new string[0];
        public string icons;
        public string groupSuffix = "_Collider";
        public string groupLabel = "collider variants";
    }

    /// <summary>The one functional link from an art pack to a tool of ours, as a capability.</summary>
    [Serializable]
    internal sealed class EnvironmentRocksTie
    {
        public string product;
        public string title;
        public string body;
        public string label = "Have a look";
    }

    [Serializable]
    internal sealed class EnvironmentRocksPipeline
    {
        /// <summary>Menu item that converts or imports the pack's materials for the active
        /// pipeline.</summary>
        public string fixMenu;
        public string materials;
        public string[] materialGuids = new string[0];
        public string[] textureGuids = new string[0];
    }

    [Serializable]
    internal sealed class EnvironmentRocksShowcase
    {
        public string id;
        public string eyebrow = "ANKLEBREAKER STUDIO";
        public string title = "Build the rest of your game";
        public string blurb;
        public string shelfTitle;
        public EnvironmentRocksPick[] shelf = new EnvironmentRocksPick[0];

        /// <summary>Art packs: the family id. The shelf is then filled from the family, and the
        /// completion card counts how much of it is installed.</summary>
        public string family;
        public string seeAll = "See all {n} assets";

        /// <summary>Filter the See all button opens the Assets tab on, and counts:
        /// <c>all</c>, <c>free</c>, a category, or <c>family:{id}</c>.</summary>
        public string seeAllFilter = "all";
    }

    [Serializable]
    internal sealed class EnvironmentRocksPick
    {
        public string id;

        /// <summary>Why this product, from the point of view of this package. Overrides the
        /// catalogue's generic blurb.</summary>
        public string pitch;
    }

    [Serializable]
    internal sealed class EnvironmentRocksStudio
    {
        public string title = "We are a small studio, and we ship what we use";
        public string body;
    }

    /// <summary>The latest devlog post, cached by the services from the studio's RSS feed.</summary>
    [Serializable]
    internal sealed class EnvironmentRocksDevlog
    {
        public string title;
        public string link;
        public string summary;
        public string date;
        public string image;
    }

    // -- Catalogue ----------------------------------------------------------

    /// <summary>The remote catalogue is authoritative; the embedded copy supports local actions offline.</summary>
    [Serializable]
    internal sealed class EnvironmentRocksCatalog
    {
        public int schema = 1;
        public string updated;
        public string publisherUrl = "https://assetstore.unity.com/publishers/101837";
        public string discordUrl = "https://discord.gg/2D36VUmsYN";
        public string devlogFeed = "https://anklebreaker-studio.com/devlog/feed.xml";
        public string careersUrl = "https://anklebreaker-studio.com/careers";
        public string consultingUrl = "https://anklebreaker-consulting.com";
        public EnvironmentRocksShowcase[] showcases = new EnvironmentRocksShowcase[0];
        public EnvironmentRocksFamily[] families = new EnvironmentRocksFamily[0];
        public EnvironmentRocksProduct[] products = new EnvironmentRocksProduct[0];
        public EnvironmentRocksGame[] games;

        /// <summary>Order, visibility and texts of the Studio tab. Absent: the built-in order.</summary>
        public EnvironmentRocksStudioLayout studio;

        /// <summary>The studio's other products, each shown as a Studio block named by its id.</summary>
        public EnvironmentRocksVenture[] ventures;
    }

    /// <summary>
    /// What the catalogue says about the Studio tab. <c>blocks</c> lists the blocks in order:
    /// <c>devlog</c>, <c>games</c>, <c>about</c>, <c>careers</c>, <c>consulting</c> or a venture id.
    /// A block left out or marked hidden is not drawn; an id this window does not know is skipped,
    /// so a later block type never breaks an older package.
    /// </summary>
    [Serializable]
    internal sealed class EnvironmentRocksStudioLayout
    {
        public EnvironmentRocksStudioBlock[] blocks;
        public string title;
        public string body;
        public EnvironmentRocksStudioCard careers;
        public EnvironmentRocksStudioCard consulting;
    }

    [Serializable]
    internal sealed class EnvironmentRocksStudioBlock
    {
        public string id;
        public bool hidden;

        /// <summary><c>tool</c> and/or <c>art</c>. Empty: every profile.</summary>
        public string[] profiles;
    }

    [Serializable]
    internal sealed class EnvironmentRocksStudioCard
    {
        public string title;
        public string body;
        public string link;
    }

    /// <summary>A studio product that is not an Asset Store package, drawn like a game card.</summary>
    [Serializable]
    internal sealed class EnvironmentRocksVenture
    {
        public string id;
        public string eyebrow;
        public string heading;
        public string intro;
        public string tagline;
        public string title;
        public string summary;
        public string cover;
        public string logo;
        public string coverUrl;
        public string logoUrl;
        public string featuresTitle;
        public string footnote;
        public EnvironmentRocksVentureLink[] links;
        public EnvironmentRocksVentureFeature[] features;
    }

    [Serializable]
    internal sealed class EnvironmentRocksVentureLink
    {
        public string label;
        public string url;
        public bool primary;
    }

    [Serializable]
    internal sealed class EnvironmentRocksVentureFeature
    {
        public string title;
        public string body;
    }

    [Serializable]
    internal sealed class EnvironmentRocksGame
    {
        public string id;
        public string name;
        public string tagline;
        public string summary;
        public string cover;
        public string logo;
        public string coverUrl;
        public string logoUrl;
        public string websiteUrl;
        public string steamUrl;
        public string steamLabel;

        /// <summary>Kept in the feed but drawn nowhere: no card, no "Used in" badge.</summary>
        public bool hidden;
        public EnvironmentRocksGameProduct[] products = new EnvironmentRocksGameProduct[0];
    }

    [Serializable]
    internal sealed class EnvironmentRocksGameProduct
    {
        public string id;
        public string role;
    }

    [Serializable]
    internal sealed class EnvironmentRocksFamily
    {
        public string id;
        public string name;

        /// <summary>Why the set is worth completing. Names no bundle and no price: none exists.</summary>
        public string pitch;
    }

    [Serializable]
    internal sealed class EnvironmentRocksProduct
    {
        public string id;
        public string name;

        /// <summary>The full store title, for the tooltip: the Card shows the short name.</summary>
        public string storeName;
        public string blurb;

        /// <summary><c>published</c>, or <c>coming-soon</c> when the store has no page for it
        /// yet. Read off the store by <c>welcome.py catalog</c>, never typed.</summary>
        public string status = "published";

        /// <summary>The store's own category path, <c>Tools / GUI</c>, <c>3D / Props / Furniture</c>.</summary>
        public string category;
        public string url;

        /// <summary>File name of the 420x280 store Card, in the package's Media/Cards folder
        /// when embedded, or under <c>cardUrl</c> once fetched.</summary>
        public string card;
        public string cardUrl;
        public bool free;

        /// <summary>Shown in FREE FROM US in every package.</summary>
        public bool pinned;
        public string family;
        public string version;
        public string releasedAt;
        public string whatsNew;

        /// <summary>Store line. A rating of 0 hides the whole line: an unknown figure is left
        /// out rather than drawn as zero stars.</summary>
        public float rating;
        public int reviews;
        public string price;

        /// <summary>A sale, as the store reports it: the pre-sale price and the percentage.
        /// Remote copy only: an embedded "-50%" would outlive the sale.</summary>
        public string originalPrice;
        public int discount;

        /// <summary>How to tell it is installed: <c>folder:</c>, <c>type:</c>, <c>asm:</c>,
        /// <c>package:</c> or <c>welcome:</c> entries, any one of which is enough.</summary>
        public string[] detect = new string[0];

        /// <summary>Menu path of its own window, for "Open its window" once installed.</summary>
        public string window;
    }
}
