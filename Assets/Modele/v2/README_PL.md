# Statek — wersja 2

Ta wersja zastępuje poprzednią wersję wizualną. Oryginał Meshy i pierwsza przebudowa zostały zachowane osobno.

## Poprawki
- Usunięte trzy maszty, ich podstawy i bocianie gniazda. Bukszpryt na dziobie pozostał.
- Pełne podparcie podwyższonego pokładu dziobowego oraz skośnych barierek; zamknięte połączenie zabudowy rufowej z pokładem.
- Usunięty jeden środkowy odcinek barierki po stronie pokazanej na drugim screenie (ujemne Y w Blenderze). Po przeciwnej stronie barierka pozostaje.
- Dodane zewnętrzne barierki obok schodów rufowych, gęstsze tralki schodowe i zamknięte podstopnice. Otwarte wejścia na podwyższone pokłady.
- Poręcze obniżone z 1,00 do 0,92 m ponad punkt odniesienia pokładu; odpowiednio dopasowane słupki i tralki.
- Koło sterowe na osi i profilowanej podporze odsuniętej za koło, z prześwitem pod obręczą. Koło pozostaje osobnym obiektem do animacji.
- Łagodniejsza krzywizna kadłuba, profilowane krawędzie, poprzeczki okien, okucia, wzmocnienia kadłuba i mały luk pokładowy.
- Deski mają płytkie łączenia, subtelne słoje i pełny ciągły podkład. Łączenia są widoczne jako detal drewna, ale nie są otworami przez pokład.
- Ciągłe obrzeże kadłuba, wspólne słupki na połączeniach barierek, poprawione styki ram okiennych i stopni.

## Pliki
- Stylized_Ship_v2.blend — model źródłowy, dwa dodatkowe LOD-y, ukryte bryły pomocnicze kolizji, światła i kamera.
- Stylized_Ship_Unity.fbx — główny model, wspólny rodzic, osobne koło i płetwa sterowa.
- Stylized_Ship.glb — alternatywny format z materiałem.
- Ship_LOD1.fbx i Ship_LOD2.fbx — uproszczenia nieruchomej części modelu.
- Ship_Palette.png — wspólna paleta kolorów 64 × 64 px; jeden materiał dla części wizualnych. Słoje są drobną geometrią.
- Ship_Collision_Guides.fbx — pomocnicze proste bryły kolizji, w tym rampy rufowe; wymagają dostosowania do rozgrywki.
- Preview*.png / Detail*.png — widoki ogólne i zbliżenia.
- mesh_statistics.json / Validation.json — dokładne liczby trójkątów i wyniki kontroli.

## Import Unity
Wstaw FBX i Ship_Palette.png do Assets. Użyj jednego materiału odpowiedniego dla renderera projektu (np. URP/Lit w URP) z paletą w Base Map / Albedo. Tekstura sRGB; filtrowanie Point. Zachowaj eksportowane normalne, aby utrzymać wygładzenie kadłuba i profilowane krawędzie.

Skala źródła: 1 jednostka = 1 metr, długość około 27,5 m. Wspólny początek modelu leży na linii wody; dziób wskazuje -X w Blenderze. Obróć wspólnego rodzica według potrzeb kontrolera statku.

Przypisz Ship_LOD0 / Ship_LOD1 / Ship_LOD2 do LODGroup. Koło i płetwa pozostają poza grupą. Początkowe progi do oceny w Twojej kamerze: 0.45 / 0.18 / 0.03.

Model jest zasobem graficznym, bez sterownika ruchu i wyporności. Konfiguracja Rigidbody, colliderów oraz poruszania gracza na pokładzie wymaga testów w docelowym projekcie. Nie przeprowadzono testu w edytorze Unity. Zweryfikowano rendery, geometrię i ponowny import FBX w Blenderze.
