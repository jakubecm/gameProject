# Props

Every model of the project. One value of the `Prop` enum (`Code/Editor/World/Props/Props.cs`) = one prefab
`Prefabs/Props/<Name>.prefab`, built by the method marked `[Model(Prop.Name)]` in its own file
`Code/Editor/World/Props/<Category>/<Name>.cs` (category = the section below; a few related props share a file:
`Storage/Boxes.cs`, `Cooling/HeatExchanger.cs`). Helpers used by several models of a category are in
`<Category>/<Category>Parts.cs` (Structure, Safety, Services, Power, Cooling); helpers and dimensions used across categories or by the rooms in `Props/Shared/`
(`Dimensions`, `Labels`, `Dials`, `Lights`, `Paths`, `TextureScales`). Mesh, material and texture helpers:
`World/Building/` (`MeshBuilder`, `ModelUtility`, `Surfaces`, `RoomPipes`, `RoomInfrastructure`, `InfrastructureModels`).
All are rebuilt in place (same GUIDs) by Tools/Cold Shutdown/Create Preview Scene; renamed or removed props are deleted.
Sizes W x H x D in metres. Where each one stands: `ROOMS.md`.

**Adding or changing a model:** add the enum value and one file with `[Model(Prop.X)] private static void BuildX(Transform root)`
(build in metres, pivot by the conventions below, one `BoxCollider`, materials via `SurfaceMaterial`, generated textures in
`Surfaces`), place it from a room file with `Put(Prop.X, ...)`, run Create Preview Scene, update this file.

## Conventions

- **Pivots:** floor props on the floor, centered, +Z = the side people use; wall props on the floor at the wall
  surface, +Z away from the wall; small wall fittings (sockets, switches, call points, grilles, readers) at the center
  of their back plate; ceiling props on the ceiling surface hanging down, long side along Z (drop-ceiling items reach
  down from the tile face, 11.2 mm); desk-top props on the desk top (0.75 m); doors on the opening pose.
- One `BoxCollider` per prop (none if out of reach), never mesh colliders.
- Smooth parts are `MeshBuilder` meshes (lathe, tube, extrusion, strip), one mesh per material, saved as
  `Art/Meshes/Details/<prefab root>_<part>.asset` (part names must be unique in a prefab). Others are ProBuilder boxes.
- Materials are shared `Art/Materials/M_<SurfaceMaterial>.mat` (URP Lit; `RackGlow` URP Unlit additive). All in-world
  text and stickers are Czech; the exit sign says "EXIT".

## Room structure

| Prop | Model | Used |
|---|---|---|
| `DoorFrame` [Structure] | casing round a 2.4 x 2.6 m opening on the room side, 0.1 m wide, 4 cm deep; does not enter the opening | every shell wall, hall door wall |
| `RoomDoor` [Structure] | bi-parting glass sliding door for a room opening; operator box, photo cells and RFID reader (55 x 125 x 20 mm, lock LED strip green/red, top edge 1.37 m, centre 1.3075 m, `Props.ReaderHeight`) on both sides, exit sign on the owner's side only; Runtime `AutoSlidingDoor` | every connected room opening (owner side), hall door walls |
| `AisleDoor` [Structure] | single black-framed glass leaf, aisle width + 2 cm, slides to local +X, jamb right, header flush with containment roof, no floor rail, reader on the right rack end; Runtime `AisleDoor` | both ends of every hall pod |
| `PassengerLift` [Structure] | evacuation lift (skirting 150 mm all round the car except the door; back wall two panels, no handrail): 1.1 x 2.1 m two-panel centre-opening door (landing + car panels move together, photo cells into the car), car 1.6 x 1.5 x 2.3 m for 6-8 people (`LiftCeiling` with the recessed `LiftCeilingLight` in the middle, operating panel), landing call panel (centre 1.26 m, top 1.35 m, below the top of a light switch), floor indicator, exit sign over the door, evacuation-lift sign | lift end |
| `LiftCeiling` [Structure] | passenger-lift car ceiling 1.6 x 1.5 m: matt brushed stainless panels 400 mm square with dark joints in exactly the grey of the car walls (`T_LiftCeiling`, material `LiftCeiling`), square 320 mm opening for the light, narrow steel edge trim; hangs from the ceiling surface | `PassengerLift` car |
| `FreightLift` [Structure] | goods-lift landing, passenger lift design in galvanised steel: 2.0 x 2.2 m chamfered two-panel door, frame, sill, call panel (centre 1.26 m, top 1.35 m, `Props.LiftCallPanelHeight`) and floor indicator; never opens, one collider | storage room east wall |

## Lighting

| Prop | Model | Used |
|---|---|---|
| `LedPanel` [Lighting] | lay-in panel 0.6 x 0.6, white 18 mm frame, opal diffuser; spot down, no shadows | offices, security office, corridors |
| `LedBatten` [Lighting] | surface tri-proof batten 1.5 m (along Z): white tray, curved opal diffuser, clips, end caps | power room, cooling plant, storage room |
| `PendantLight` [Lighting] | `LedBatten` hanging from the deck on two wire ropes 1.1 m apart (ceiling plates, cups, ferrules, thimbles), top at 4.1 m, black power cable from a roof junction box | hall walkways along the walls |
| `PendantLightGirder` [Lighting] | the same fitting hung from a girder's bottom flange (5.2 m) by two beam clamps, top at 4.1 m, junction box under the flange (`PendantLight.cs`) | hall walkways between tiles |
| `AisleLight` [Lighting] | low-profile LED luminaire 1.2 x 0.07 m, 12 mm deep: tray with chamfered corners and a bevelled lower edge in `RackBlack` (the colour of the containment roof), diffuser with chamfered corners 2 mm proud, no clips, two spots, under the containment roof | hall pods (2 each) |
| `LiftCeilingLight` [Lighting] | the one light of the lift car, recessed in its ceiling like a lay-in panel: 300 x 300 mm LED panel, opal diffuser 12 mm up in the void, satin stainless bezel 30 mm wide with a 14 mm reveal; spot down, no shadows | `PassengerLift` car |
| `ExitSign` [Lighting] | 360 x 160 x 50 mm white housing, emissive green "EXIT" face (`T_ExitSign`) | on `RoomDoor`, `PassengerLift` |

## Safety and security

| Prop | Model | Used |
|---|---|---|
| `Sprinkler` [Safety] | pendent glass-bulb sprinkler 63 mm: chrome escutcheon, hex body, brass ring, red bulb, frame arms, 16-tine deflector | corridors, small rooms |
| `SmokeDetector` [Safety] | 100 x 46 mm glossy white, status lens, mesh chamber, domed cap | corridors, small rooms, hall deck |
| `CeilingSpeaker` [Safety] | recessed PA speaker 216 mm, white trim, domed grille 185 mm (`T_SpeakerGrille`) | corridors, offices (not the hall, not technical rooms) |
| `FireExtinguisher` [Safety] | 6 kg ABC powder, Ø160 mm, ~0.6 m on its bracket, label `T_ExtinguisherLabel`, chrome head with gauge, hose to a nozzle | corridor fire arm, every small room |
| `FireCallPoint` [Safety] | 92 x 92 x 52 mm red manual call point, push symbol, fire pictogram, top edge at 1.37 m (centre 1.324 m) | corridor fire arm, small rooms, hall walls |
| `HoseCabinet` [Safety] | 0.65 x 0.65 x 0.24 red hose cabinet, window with gasket, white hose on a red reel, bottom at 0.9 m | corridor fire arm |
| `GasNozzle` [Safety] | 360° inert-gas nozzle | hall gas lines (2 per pod) |
| `LightSwitch` [Services] | 80 x 80 mm plate, 56 mm rocker, 11 mm proud, top edge at 1.37 m like `FireCallPoint` and the readers (centre 1.33 m, `Props.LightSwitchHeight`) | corridors, small rooms, hall door walls |
| `CameraDome` [Safety] | ceiling dome camera 111 x 80 mm, clear dome over a black inner dome; feed 30° down; Runtime `SecurityCamera` | corridor crossings/T/corners, small rooms |
| `CameraPendant` [Safety] | junction box clamped to a beam, pole with cable, dome at its end (dome at 3.0 m) | hall girders (2 per open east edge) |

## Building services

| Prop | Model | Used |
|---|---|---|
| `PowerSocket` [Services] | flush 230 V type E on 80 x 80 plate, 10 mm proud | offices, security office, corridor arm 1, kitchenette, printer |
| `DataSocket` [Services] | flush double RJ45 on an 80 x 80 mm plate, 12.5 mm proud: real jack openings with latch notch, black inside, contact carrier and 8 gold contacts (same helpers as the rack ports), raised surrounds, two cross-head screws, blue label strips | corridor arm 1, terminal outlets, security racks |
| `FloorBox` [Services] | 150 x 150 mm floor box, pewter plate, hinged lid, screw lock; under the back of the desk it feeds | security office consoles |
| `WallVentGrille` [Services] | 600 x 300 louvred grille under the ceiling | technical rooms back wall (2) |
| `FloorVentGrille` [Services] | 600 x 600 louvred grille 0.15 m above the floor | one per corridor cell |
| `CeilingDiffuser` [Services] | 600 mm four-way diffuser, nested square cones | offices, security office |
| `PerforatedFloorTile` [Services] | 0.6 x 0.6 perforated raised-floor tile, 12 mm proud: powder-coated steel in the hall-floor grey (`HallFloor`), staggered 25 mm holes with chamfered rims, paint grain and a dark plenum in the holes (`T_FloorTilePerforated`, 4 x 4 holes per 0.1 m) | hall cold aisles |
| `PipeThermometer` [Services] | dial thermometer on a brass stem, dial +Z | cooling-plant pumps |

Non-prop services (built inside rooms by `RoomInfrastructure` / `RoomPipes` / `InfrastructureModels`): pipe risers with
strap clamps, rosettes and ball valves (lever grip = pipe colour: red fire, green water, yellow gas); insulated wide pipes
(aluminium, coloured bands); trunking 120 x 60 mm with mitred corners; cable trays (galvanised, slotted, cables, M10
rods, strut trapeze); rectangular ducts (aluminium/stainless, flanges, hangers), bellows, collar frames, duct grilles;
painted floor lines and hatching; conduits (painted tube, chrome rosette).

## Office and security furniture

| Prop | Model | Used |
|---|---|---|
| `Desk` [Office] | white panel desk 1.6 x 0.75 x 0.8, 25 mm top, side panels, modesty panel | office (1), security office (2) |
| `OfficeChair` [Office] | black swivel chair, five-star base, quilted lower backrest (`T_ChairQuilt`), loop armrests; 0.58 wide, seat 0.47, 1.05 tall | at every desk and meeting table |
| `Workstation` [Office] | two 24" monitors 0.55 m apart angled in, keyboard (~100 keys, numpad), mouse; monitor = slim body, lit picture `T_MonitorScreen`, neck, silver base | every desk |
| `PcTower` [Office] | mid-tower 0.2 x 0.45 x 0.45, Dell OptiPlex-like front, diamond mesh `T_PcVent` | beside every desk |
| `MeetingTable` [Office] | 1.8 x 0.75 x 0.9, light 30 mm top on two T-legs | office |
| `FilingCabinet` [Office] | five-drawer 0.45 x 1.32 x 0.6, white steel, finger pockets | office (2) |
| `Printer` [Office] | A3 copier on a 4-drawer cabinet 0.6 x 1.27 x 0.65, scanner with feeder, control panel | office |
| `WallTv` [Office] | 65" 1.46 x 0.84 x 0.028, centre at 1.6 m | office back wall |
| `VideoScreen` [Office] | 55" display on a wall bracket; screen quad for a camera feed (`SecurityMonitor`) | security office video wall (8) |
| `CoffeeCounter` [Office] | kitchenette 1.8 m: white gloss base units (3 x 0.6), stone top at 0.9, dark double sink, gooseneck tap, wall cabinets | office |
| `CoffeeMachine` [Office] | bean-to-cup 0.19 x 0.34 x 0.48, graphite, copper band, espresso cup | on the counter |
| `Fridge` [Office] | fridge-freezer 0.6 x 1.8 x 0.65, white, silver D-handles | office |
| `Mug` [Office] | white ceramic mug 83 mm, triangular handle | counter, desks, meeting table |
| `PaperStack` [Office] | 16 loose A4 sheets, top sheet `T_PaperPage` | desks, cabinets, meeting table, security "ShiftLog" |
| `PhotoFrame` [Office] | 15 x 20 cm black mitred frame, mount, `T_PhotoPicture` behind glass, hinged leg | office desk |
| `Locker` [Office] | two-door steel locker 0.6 x 1.8 x 0.5, grey carcass, slate doors, vents, cylinder locks | security office (2) |
| `LockerBench` [Office] | 1.5 x 0.38 x 0.45, three oak slats on a cream tube frame | security office |

## Storage

| Prop | Model | Used |
|---|---|---|
| `Shelf` [Storage] | boltless galvanised shelving 1.0 x 1.8 x 0.4, slotted angle posts, 4 chipboard boards in C channels (board tops 0.10 / 0.62 / 1.14 / 1.66) | storage room (15) |
| `BoxLidCube` [Storage] | lidded box 0.28 x 0.28 x 0.28 | shelves |
| `BoxLidFlat` [Storage] | lidded box 0.28 x 0.14 x 0.28 | shelves |
| `BoxCartonCube` [Storage] | shipping carton 0.30 cube, flap seam, handle holes | shelves |
| `BoxCartonFlat` [Storage] | shipping carton 0.30 x 0.17 x 0.30 | shelves |

## Power room

| Prop | Model | Used |
|---|---|---|
| `Transformer` [Power] | cast-resin transformer in enclosure 2.0 x 2.2 x 1.2: louvers, inspection window (core + 3 coils), control panel, rating plate, HV warning, cable duct to the ceiling | power room (2) |
| `Switchgear` [Power] | LV switchgear 0.8 x 2.2 x 0.6: instrument door with control panel, 3 breaker doors (molded-case breaker, rotary knob), busbar duct up; no meters | power room (2) |
| `UpsCabinet` [Power] | UPS 0.6 x 2.0 x 0.8 graphite: two vents, control panel, tag, green status strip, handle | power room (2) |
| `PowerMeterPanel` [Power] | 6 large kW dials (`T_PowerDial`) in 2 x 3 on a rounded board, trunking to the ceiling; clue display (`AnalogGauge` planned) | power room back wall |

## Cooling plant

| Prop | Model | Used |
|---|---|---|
| `HeatExchanger` [Cooling] | 2.2 x 2.0 x 0.9 aluminium-clad, diamond-folded front, control panel, blue channel skid | cooling plant east |
| `HeatExchangerMirrored` [Cooling] | same, secondary pipes at the other end | cooling plant west |
| `CirculationPump` [Cooling] | end-suction pump 1.0 x 0.7 x 0.5: cast-iron volute, blue ribbed motor, tapered feet, skid; pivot = discharge axis | cooling plant (2) |
| `BufferTank` [Cooling] | Ø0.7 x 2.2 on three legs, aluminium clad, green bands, two top nozzles | cooling plant |
| `AirHandler` [Cooling] | CRAH unit with duct to the ceiling and a damper | cooling plant |
| `ExpansionVessel` [Cooling] | Ø0.44 x 1.1 white vessel on three legs, blue cap, yellow sticker, side nozzle | cooling plant |
| `CoolingMeterPanel` [Cooling] | 6 narrow pipes from the floor with 3x life-size dial thermometers (`T_ThermometerDial`), into the exchangers' end faces; clue display | cooling plant back wall |

Stickers and plates are quads 3 mm off the surface showing cells of `T_PlantLabels.png` (4 x 4 Czech atlas: plant
labels, transformer plate, switchgear tag, UPS tag; material `PlantLabels`).

## Server hall

| Prop | Model | Used |
|---|---|---|
| `Rack` [Hall] | 42 U rack 0.6 x 2.0 x 1.2, only the front modelled; Runtime `Rack.Configure(openable, seed)`. Closed: fixed door, flush handle, no interaction. Openable: pulled-out lever handle, door ajar, `RackDoor` on the door hinge (F opens fully / shuts; only looking at the door offers it, the body's collider stops behind the door), `RackGlow` (subtle breathing white-beige outline quad `DoorGlow`, `T_RackGlow`, lit only while the door is shut perfectly). Door: hex grille `T_RackGrille` (alpha, mipmapped, anisotropic) | every hall rack slot and the 2 security-office slots (placed by `RackPlacement`, not in prefabs; its openable rack is never the first or last of its run of racks, except in the 2-rack security office) |
| `HallSign` [Hall] | white cube on two rods (bottom at 4.2 m), the tile code on all four faces (`HallSign.Code`) | one per hall tile |

**Rack fillings:** 12 deterministic random 42 U fillings (`ContentVariants`) as shared meshes in `Art/Meshes/Racks/`,
listed in `Data/RackContents.asset` (`RackContentLibrary`); a rack picks one by its seed. The servers are the six items below (one
file each in `World/Props/Hall/RackItems/`, shared hole / port / symbol helpers in `RackItemParts.cs`: ports are real holes cut into plates by `PlateWithHoles` with an inside from `RackParts.Cavity`, chosen by `RackItem`; all 19" wide, fronts at `ServerFront`, built as geometry plus generated
textures, never prefabs). A filling is: maybe a UPS at the bottom (70 %), then a random mix of the servers with the odd blanking panel and at
most one switch (never at the top); the top unit is always a server (disk or closed); no PDUs, routers or cables; status LEDs show through the grille.

| Rack item | Model |
|---|---|
| `DiskServer2U` | disk server 2U after an HPE ProLiant DL380 Gen10 (24 SFF): three silver-outlined groups of 8 separate vertical trays (lighter face, slotted lever, label tab, release button, dark red window; 2.5 mm black void between trays), a real VGA port on the left ear (stepped D opening, blue insulator with 15 pin holes, jack screws), control panel with power button, health / network / UID LEDs each beside its symbol, USB-A and iLO RJ45 as real holes, logo and model plate and drive-box tag on the ears |
| `DiskServer1U` | disk server 1U (SFF, ProLiant DL360 type): two rows of five separate horizontal drive trays (lighter face, slotted handle, release button, red window; black void between trays and rows), real VGA port on the left ear, control panel with power button, LEDs beside their symbols, USB-A and iLO RJ45 holes |
| `ClosedServer3U` | closed server 3U after an Inter-Tech 3U chassis: two drive-bay covers with punched slot-and-hole rows (`T_ChassisVent`), lock panel with key lock, power and disk LEDs beside their symbols and two USB 3.0 ports as real holes (blue tongues), rack handles with ribbed grips |
| `Switch1U` | 48-port PoE switch 1U (TP-Link Omada type): dark grey faceplate (`SwitchFace`) with real holes for every port: USB-A, RJ45 console, micro-USB, 2 x 24 RJ45 (latch notch, gold contacts, groups of eight separated by a gap) and four SFP+ cages; each port's link LED at the port (top row above, bottom row below), system LEDs with symbols at the left |
| `Ups2U` | rack UPS 2U after an Eaton 9E: honeycomb panels, black gaps round the control module with blue LCD (`T_UpsDisplay`: "ONLINE MODE / OUT:230V 49.9HZ") and five buttons, make plate |
| `BlankPanel1U` | blanking panel 1U: matt black steel, folded lips, cage-nut slots with rack screws |

## Placement rules

| Thing | Only in |
|---|---|
| shelving, cardboard boxes | storage room |
| lockers, locker bench | security office |
| transformers, switchgear, UPS | power room |
| heat exchangers, pumps, tanks, air handler, expansion vessel | cooling plant |
| racks | hall pods, security office (2) |
| coffee corner, fridge, TV, meeting table, printer, filing cabinets | office |
| on desks | monitors, keyboard, mouse, papers, mugs, photo — never boxes, binders or laptops |
| never anywhere | service carts, laptops, cable drums, binders, emergency lights, decorative/hinged doors, roller shutters |

## Textures (`Art/Textures`)

Generated by `Surfaces` (`World/Building/Surfaces.cs`) (rewritten by every Create Preview Scene): `T_PcVent`, `T_ExtinguisherLabel`,
`T_SpeakerGrille`, `T_RackGrille`, `T_TraySlots`, `T_RackGlow`, `T_CeilingTile600`, `T_LiftCeiling`, `T_FloorTilePerforated`,
`T_ServerHoneycomb`, `T_ChassisVent`, `T_UpsDisplay`. Drawn files (edit the PNG): `T_MonitorScreen`, `T_ChairQuilt`, `T_PaperPage`,
`T_ExitSign`, `T_PhotoPicture`, `T_ThermometerDial`, `T_PowerDial`, `T_PlantLabels`. `T_RackPerforation` is unused.

## Surface materials (`SurfaceMaterial`, append only; agree on new slots with the other developer)

Floor FloorVent Wall Ceiling Trim Metal Furniture Upholstery Screen LightPanel MugCeramic SafetyRed CeilingGrid Trunking
PowerCable DataCable DataTray PipeWater PipeGas HallFloor HallWall Steel RoofDeck SafetyYellow SafetyGreen Glass SignFace
WireMesh LedRed Chrome Brass BulbRed BulbClear BlackHousing DomeClear LensGlass SpeakerWhite SpeakerGrille DetectorWhite
DetectorMesh CeilingTile ReaderBlack ExtinguisherLabel HoseRed HoseWhite KeyboardGraphite KeyboardKey MouseBlack PcVent
MonitorImage ChairLeather ChairQuilt PaperPage ExitSignFace FloorBoxMetal StainlessSteel OfficeTopLight OfficeLegWhite
DeskWhite StoneTop CopperTrim CoffeeGraphite CoffeePanel CabinetWhite SmokedTank CabinetPull LockerBody LockerDoor
LockerPlate BenchWood BenchFrame PhotoPicture ValveNickel ValveSteel ValveGrip PipeClamp AluminiumClad MotorGreen
SupportBlue DialFace PlantLabels CastIron CabinetGrey GalvanisedSteel ShelfBoard Cardboard CardboardDark CardboardEdge
ElectricGrey UpsGraphite ResinCoil PowerDialFace RackBlack RackMesh ServerBlack ServerSilver DriveBay LatchRed
LedAmber ServerHoneycomb ChassisVent ServerVoid UpsDisplay SwitchFace TrayPerforated FloorTilePerforated Concrete DuctStainless ConduitViolet
ConduitMagenta RackHandleLight CeilingTile600 RackGlow ConduitBrown ConduitGreen LiftCeiling
