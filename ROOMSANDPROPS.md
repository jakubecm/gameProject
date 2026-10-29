# Místnosti a vybavení

Jak má vypadat každá místnost datacentra a co v ní smí stát. Rozměry jsou v metrech
(š × v × h = šířka × výška × hloubka), podlaha je ve výšce 0. Čísla, která používá kód, žijí
v `RoomContract` a `HallContract`; tady jsou jen pro představu.

---

## 1. Pravidla, která platí všude

**Buňka a stěny.** Každá místnost zabírá buňku 12 × 12 m se stropem ve 3 m (hala 6 m). Stěny jsou
0.2 m silné a stojí uvnitř buňky, takže vnitřek měří 11.6 × 11.6 m. Uprostřed každé strany je otvor
2.4 × 2.6 m; nepoužitý otvor zazdí generátor a zmizí u něj i rám dveří, takže vypadá jako zbytek stěny.

**Malé místnosti jsou čtverec 6 × 6 m.** Malá kancelář, velín, rozvodna, strojovna i sklad jsou striktně
čtvercové (6 × 6 m na osách zdí, 5.8 × 5.8 m uvnitř) u jižních dveří buňky; zbytek buňky je zastavěný.
Mají vždy jen jedny dveře. Jedinou velkou místností (celá buňka) je velká kancelář.

**Čtyři pevné rozložení.** Každá místnost má 4 rozložení (prefaby `Room_<Typ>`, `Room_<Typ>_Layout2–4`),
generátor vybere jedno na buňku. Rozložení 1 a 2 jsou dva různé návrhy, 3 a 4 jsou jejich zrcadla
(malé místnosti zrcadlené východ–západ, velká kancelář podle úhlopříčky, takže si vymění dveře jih
a západ). Všechny se dají projít ve scéně `TestPreview`. Chodby se
liší tím, ve kterém rameni je požární výbava: roh má 2 varianty (druhá je zrcadlo), T 4 (výbava
v bočním rameni nebo v kmeni a jejich zrcadla); generátor vybere tu, jejíž požární rameno vede
do místnosti.

**Přístup a dveře.** Do každé místnosti se dá dojít a každé dveře fungují. Vstup do místnosti tvoří
automatické posuvné skleněné dveře se čtečkou z obou stran. Exit cedule visí jen na straně místnosti
(na krytu pohonu dveří) a ukazuje cestu ven do chodby; na straně chodby exit cedule být nesmí (požární
předpisy). Atrapy dveří (dveře na pantech, rolovací vrata) se nepoužívají – jedinou výjimkou je
nákladní výtah ve skladu, jehož plné dveře se nikdy neotevřou a mají před sebou žlutě šrafovaný
prostor, který se nesmí zastavět. Osobní výtah ústí přímo do chodby; jeho dveře se otevírají samy.

**Volné cesty.** Od každých dveří vede do středu místnosti volný pás široký 2.4 m až do výšky 2.6 m
(nad ním smí být světla a žlaby). V malé místnosti stačí 2.2 m ode dveří. Vedle dveří zůstává stěna
volná 1.2–2.7 m od osy otvoru (0.3 m od zdi, do 2.95 m), tam zajíždějí křídla a visí čtečka.

**Kudy se chodí do velké kanceláře.** Má dveře jen na dvou sousedních stranách (v prefabu jih
a západ, generátor ji natočí). Cesty od dveří se potkají uprostřed a druhá polovina místnosti zůstane
celá pro nábytek. Kolem stolů a židlí je vždy místo na průchod (za židlí ještě aspoň 0.4 m).

**Podhled.** Kanceláře, velín a chodby mají viditelný rastr 600 × 600 mm těsně pod stropem. Jedna
dlaždice je vždy přesně uprostřed místnosti, takže světlo v chodbě visí uprostřed její šířky. Vše na
stropě sedí v rastru: věci 600 mm na středu dlaždice, panely 1200 mm přes dvě dlaždice. Technické
místnosti (rozvodna, strojovna, sklad) podhled nemají a mají epoxidovou podlahu.

**Rozvody.** Každá trubka, lišta, chránička i žlab začíná a končí ve zdi, ve stropě, v podlaze nebo
v přístroji – nikdy nekončí ve vzduchu. Zásuvky na chodbách a v technických místnostech jsou v krabici
na zdi, do které vede chránička svisle ze stropu. Barvy: červená = požární voda, zelená = chladicí
a užitková voda, žlutá = plyn, žlutý žlab = data, kovový žlab s černými kabely = elektřina.

**Co je vidět.** Kanceláře a velín schovávají rozvody nad podhledem a v parapetní liště. Chodby ukazují
stoupačky a zásuvkové krabice, ale žádné žlaby, jističe ani vzduchotechnické potrubí. Technické
místnosti ukazují, co k nim patří (žlab v rozvodně, potrubí a přívodní kanál ve strojovně).

**Větrání.** Velké mřížky jsou čtvercové 0.6 × 0.6 m (člověk by se jimi právě protáhl) dole
u podlahy, malé mřížky (0.6 × 0.3 m) nahoře pod stropem. Spodní mřížka začíná 0.15 m nad podlahou,
horní končí 0.15 m pod podhledem (v hale pod nosníky) – stejně v místnostech, chodbách i v hale.
Každá buňka chodby má právě jednu mřížku, vždy tu velkou dole, uprostřed své stěny (v rameni naproti
požárnímu, nebo na zdi, která ho zazdívá), nikdy u požární výbavy. Kanceláře a velín
mají místo mřížek stropní vyústky v podhledu.

**Požár a bezpečnost** (podle běžné praxe NFPA 13 / ČSN EN 12845, EN 54). Všude kromě haly sprinklery
nejvýš asi 4 m od sebe a 2.3 m od zdi – v chodbě jedna řada v ose se světly, v malé místnosti čtyři,
ve velké kanceláři devět. Kouřový hlásič jeden na buňku chodby a malou místnost, tři ve velké
kanceláři. U východu ruční hlásič a hasicí přístroj, nouzové svítidlo. V chodbě stojí hydrant,
hasicí přístroj a ruční hlásič vždy v rameni, které vede do místnosti (u jejího vstupu, mimo hlavní
chodbu). Kamery: v chodbě u středového světla, dvě na křižovatce a T, jinak jedna; malá místnost
jedna v rohu u dveří, velká kancelář dvě (v zasedačce žádná). V podhledech reproduktory evakuačního
rozhlasu. Vypínače světel (evropské čtvercové 80 × 80 mm) co nejméně, ale všude: u každých dveří
místnosti jeden, u každých dveří haly jeden, v každé buňce chodby jeden, vycentrovaný nad mřížkou.
Hala hasí jen inertním plynem (bez vody).

**Světla.** Málo světel kvůli výkonu, žádné tmavé kouty, žádné stíny: velká místnost 6–10 svítidel,
malá místnost 3–10, chodba jedno na rameno a jedno na křižovatce.

**Rozestupy u strojů.** Mezi stroji alespoň 1 m, ke zdi alespoň 0.6 m (k transformátoru 0.8 m),
před rozvaděči a skříněmi 1.2 m pracovního prostoru.

---

## 2. Kde smí co stát

| Věc | Smí stát |
|---|---|
| Regály s krabicemi a šanony | jen ve skladu |
| Šatní skříňky a lavice | jen ve velínu |
| Na stole | monitor nebo dva, klávesnice, myš, papíry, hrnek, fotka – nikdy šanony |
| Racky | v hale a nejvýš dva ve velínu |
| Transformátory, rozvaděče, UPS | jen v rozvodně |
| Výměníky, čerpadla, nádrže | jen ve strojovně |
| Skleněné příčky | jen kolem zasedačky ve velké kanceláři, nikdy v hale |
| Servisní vozík, notebook, cívka kabelu | nikde (zrušeno) |

---

## 3. Předměty

**Nábytek kanceláře a velínu.** Stůl 1.6 × 0.75 × 0.8 s PC skříní pod deskou a kancelářskou židlí;
na stole pracoviště s jedním monitorem nebo dvojmonitor (klávesnice, myš vpravo); stůl uprostřed
místnosti napájí malá podlahová krabice 150 × 150 mm schovaná pod zadní částí desky. Jednací stůl
1.8 × 0.9, židle jen po dlouhých stranách. TV i whiteboard mají střed ve stejné výšce (1.6 m). Kartotéka 0.45 × 1.32 × 0.6. Multifunkční tiskárna
0.6 × 1.15 × 0.65. Nástěnná TV, whiteboard. Kávový kout: kuchyňská linka 1.8 m s dokonale uzavřenou spodní skříňkou,
nerezovým dřezem s hlubokou vanou a pákovou baterií, horní skříňky, kávovar, hrnky, lednice 0.6 × 1.8 a vysoký
kulatý stolek na stání (Ø 0.7, výška 1.05). Šatní skříňka 0.4 × 1.8 × 0.5 a lavice (jen velín).
Skříň UPS 0.6 × 2.0 × 0.8 se skleněnými dvířky (rozvodna).

**Sklad.** Ocelový regál 1.0 × 2.0 × 0.4 s pěti policemi, archivními krabicemi, plastovými přepravkami
s díly a šanony. Nákladní výtah (jen vzhled): plné ocelové teleskopické dveře 2.0 × 2.2 v těžkém rámu
se žlutočerným pruhováním, práh, nárazové lišty, ukazatel patra, tlačítka s klíčkem a štítek nosnosti.

**Osobní (evakuační) výtah.** Kabina pro 6–8 lidí s rezervou, 1.6 × 1.5 m, výška 2.3 m (široká
kabina podle ISO 4190), dvoukřídlé středové nerezové dveře 1.1 × 2.1, které se samy otevřou, když hráč přijde ke
dveřím (zvenku i zevnitř kabiny). V kabině zrcadlo, madla, gumová podlaha, svítící strop a ovládací
panel; venku nerezový rám, tlačítka přivolání, ukazatel patra, exit cedule nad dveřmi, značka
evakuačního výtahu a požární spínač pro hasiče.

**Technika.** Transformátor 2.0 × 2.2 × 1.2 se žaluziemi a kabelovou lištou do stropu. Rozvaděč NN
0.8 × 2.2 × 0.6 s jističi a ampérmetrem. Malý podružný rozvaděč na zdi. Panel šesti analogových
budíků (jejich ručičky jsou stopy pro hráče). Deskový výměník 1.4 × 1.6 × 0.6 se čtyřmi
přírubami a potrubím za sebou až do stropu. Oběhové čerpadlo, akumulační nádrž chladicí vody
Ø 0.8 × 2.2, teploměry na trubkách.

**Racky** 0.6 × 2.0 × 1.2: compute, GPU, JBOD a network; uzavřené boky, záda i vršek, zařízení na čele.

**Rozvody a zabezpečení.** Stoupačky s objímkami a ventily, kabelové žlaby na závitových tyčích,
lišta 120 × 60 mm, zásuvky 230 V a RJ45, podlahové krabice, mřížky, stropní vyústky, sprinklery,
hlásiče, hydrantová skříň, hasicí přístroj, ruční hlásič, plynová stanice, nouzová svítidla, exit
cedule, vypínače světel, stropní reproduktory, kamery (stropní, nástěnné, závěsné – vždy s
polokulovým krytem), LED panely, lišty a závěsná halová svítidla.

---

## 4. Místnosti

### Chodba
Ramena široká 3 m mezi plnými bloky; chybějící rameno je zazděné. Tvary: křižovatka, T, rovinka,
roh, slepý konec. V ose chodby: světlo uprostřed křižovatky a v každém rameni, mezi nimi sprinklery
(1.8 a 4.8 m od středu). Jeden hlásič, jeden reproduktor a jedna mřížka na buňku; kamery u středového
světla. Ramena si dělí rozvody: v požárním rameni (vede do místnosti) požární stoupačka
s hydrantovou skříní, hasicí přístroj a ruční hlásič; v dalším stoupačky vody a plynu a krabice se
zásuvkami napájená shora (vše na jedné stěně); v posledním nouzové svítidlo. Mřížka u podlahy a nad
ní vypínač uprostřed stěny naproti požárnímu rameni. Žádné viditelné vzduchotechnické potrubí.

### Velká kancelář (celá buňka, dveře jih a západ)
- **Návrh 1:** prosklená zasedačka 4.3 × 4.3 m v severovýchodním rohu (stůl pro šest, TV a whiteboard
  ve stejné výšce), blok čtyř stolů kolmo k severní zdi s parapetní lištou, tiskárna a kartotéky,
  kávový kout u východní zdi (linka, lednice, stolek na stání), dva stoly zády k sobě s kartotékami,
  na jihozápadě stolek pro čtyři, terminál a tiskárna.
- **Návrh 2:** otevřené jednací místo na severozápadě (stůl pro šest, TV na severní a whiteboard na
  západní zdi), kávový kout na severní zdi s kartotékami, „bench“ čtyř stolů na jihovýchodě a stůl na
  chvíli u východní zdi, na jihozápadě terminál, tiskárna a kartotéky.

### Malá kancelář (6 × 6 m)
- **Návrh 1:** dva stoly čelem k západní zdi, kávový kout na zadní zdi se stolkem na stání, tiskárna
  a kartotéky na východní zdi, terminál u dveří.
- **Návrh 2:** jednací stůl pro čtyři kolmo k TV na zadní zdi, dva stoly čelem k východní zdi,
  kartotéky, tiskárna a terminál na západní zdi.

### Velín (6 × 6 m)
Jen to, co k němu patří: video stěna 2 × 4 obrazovek přes celou zadní stěnu, před ní dva stoly
s dvojmonitory, dva racky (compute a JBOD, může v nich sedět AI) na boční zdi a terminál u dveří.
Zbylé místo: v návrhu 1 šatna (tři skříňky a lavice), v návrhu 2 kávový kout. Méně světel, aby byly
obrazovky čitelné.

### Rozvodna (6 × 6 m)
Dva transformátory podél bočních zdí v severní polovině (0.8 m za nimi, 1.8 m mezi čely), každý za
žlutou ochrannou čarou; nasávací mřížka dole za západním, odtah nahoře na východní zdi. Na zadní zdi
mezi nimi panel analogových budíků všech sekcí haly. V jižní polovině 1 m od transformátorů: návrh 1
dva rozvaděče NN na západní a dvě skříně UPS na východní zdi, návrh 2 dva podružné rozvaděče na
západní a rozvaděče NN na východní zdi. Přiznaný kabelový žlab přes místnost, CO₂ hasicí přístroj.

### Strojovna chlazení (6 × 6 m)
Dva deskové výměníky (návrh 1 u zadní zdi, návrh 2 podél bočních zdí), dvě oběhová čerpadla se
stoupačkami a teploměry, akumulační nádrž, panel budíků (teplota chladiva, průtok vzduchu), dvojice
stoupaček chladicí vody na zdi a přívodní kanál pod stropem přes místnost, odtahová mřížka nahoře.

### Sklad (6 × 6 m)
Zaplněný regály s přepravkami, krabicemi a šanony, ke každému regálu vede ulička aspoň 0.9 m: řada
podél zadní a západní zdi, dvojitá řada zády k sobě uprostřed (návrh 1 podél Z, návrh 2 podél X)
a regál na východní zdi. Na východní zdi nákladní výtah (nefunkční), před ním žlutě šrafovaný prostor.

### Výtahový konec chodby
Žádná mezimístnost: zvláštní dlaždice chodby jako slepý konec (jediné rameno), jejíž uzavřený konec
tvoří přímo stěna výtahové šachty s osobním (evakuačním) výtahem, za ní šachta s kabinou; v kabině
začíná každá hra. Chodba je vybavená jako ostatní (světla, sprinklery, hlásič, hasicí výbava
v rameni), mřížka u podlahy a vypínač uprostřed západní stěny křižovatky. Generátor ji přidá právě
jednou, jako navazující dlaždici do slepého konce nejblíž středu mapy.

### Hala
Jen infrastruktura: žádné stroje, rozvaděče, zásuvky, požární voda ani příčky. Hala je vždy jeden
obdélníkový prostor bez dělicích stěn (v náhledech 3 × 3 dlaždice, uzly A1–C3).
- **Dlaždice 12 × 12 m, strop 6 m.** Přes šířku dlaždice stojí dvě uličky (severní a jižní), každá
  je uzavřený kontejnment: dvě řady po 14 rackách čely proti sobě přes studenou uličku 1.2 m, všechny
  pozice obsazené racky, podlaha z perforovaných mřížových dlaždic, střecha nad racky a automatické
  dveře na obou koncích – dovnitř se dá jen dveřmi. Záda obou uliček se dívají do teplé uličky
  uprostřed (1.4 m).
- **Volné cesty.** Ke zdem na koncích uliček 1.6 m, k podélným zdem 1.5 m; mezi sousedními
  dlaždicemi tak vznikají široké průchody.
- **Nad hlavou** (vše nad cedulí): odvodní kanál nad teplou uličkou, nad každou uličkou přívodní kanál
  z centrální strojovny se svodem do střechy kontejnmentu, elektro a datový žlab s jedním svodem
  a plynové potrubí s tryskami, kouřové hlásiče, závěsná světla nad průchody, kamery na nosnících
  a jediná cedule – bílá kostka s kódem dlaždice (např. A3) nad uličkami.
- **Obvodová stěna** 6 m nese velké přívodní mřížky 0.15 m nad podlahou (jako v chodbách) a malé odvodní nahoře 0.15 m pod nosníky,
  plynovou stanici, ruční hlásič a kameru; plná stěna navíc plynovou stoupačku, vnější roh kabelovou
  stoupačku; se dveřmi navíc automatické dveře, čtečky, exit cedule a vypínač.
