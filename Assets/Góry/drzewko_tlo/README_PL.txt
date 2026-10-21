KOLOSALNE DRZEWO — na bazie przeslanego Tree.prefab

Tree_Kolos_450m.blend — edytowalny plik Blendera; tekstury spakowane.
Tree_Kolos_Unity.fbx — eksport siatek w jednostkach metrycznych, bez studia.
Tree_Kolos_podglad.png / Tree_Kolos_bok.png — podglady.

Wykorzystano oryginalna siatke i UV drzewka Tree z Retro Shaders Pro:
przeksztalcony glowny pien, 38 rozbudowanych kopii jego czesci drewnianej
i 292 mniejsze skupiska listowia wyodrebnionego z tego samego modelu.
Dodano masywny szkielet pnia oraz rozchodzace sie korzenie podporowe.
Materialy pozostaja rozdzielone: Retro Tree Bark i Retro Tree Leaves.
Kora w podgladzie uzywa barwienia oryginalnego materialu drzewka.

Wysokosc ok. 462 m, szerokosc ok. 408 m. Jedna jednostka = jeden metr.
332 osobne obiekty, ok. 115 tys. trojkatow. Galezie i korzenie przecinaja
pien u nasady, nie sa zlaczone Booleanem w jedna szczelna bryle.
Model nie ma LOD, colliderow, riggu ani animacji wiatru.

UNITY / RETRO SHADERS PRO
Import Scale Factor = 1 z wlaczona konwersja jednostek.
Przypisz istniejace materialy Retro Tree Bark i Retro Tree Leaves z assetu.
Shader: Retro Shaders Pro/Retro Lit.
Resolution Limit = 128/256 dla mocniejszej pikselizacji; Filter Mode = Point.
Texel Lit dopasowuje oswietlenie do texeli. Snapping i affine mapping sa opcjonalne.
Obrys to osobny shader Retro Outline na dodatkowej warstwie geometrii;
sam shader Retro Lit nie dodaje konturu. Grubosc obrysu dobieraj do skali modelu.
Standardowy Retro Lit nie ma osobnych slotow roughness, AO i height.

Poprzednie drzewko i modele aren nie zostaly nadpisane.
