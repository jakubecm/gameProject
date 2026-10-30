# Props

Every model of the project. One enum value of `GrayboxProp` = one prefab `Prefabs/Props/<Name>.prefab`, built by
`GrayboxPropsTool*.cs` (file in brackets) and rebuilt in place (same GUIDs) by Tools/Cold Shutdown/Create Preview
Scene. Sizes W x H x D in metres. Where each one stands: `ROOMS.md`. Replace a graybox with a final model by editing
its prefab (task 4.7); rooms hold prefab instances.

## Conventions

- **Pivots:** floor props on the floor, centered, +Z = the side people use; wall props on the floor at the wall
  surface, +Z away from the wall; small wall fittings (sockets, switches, call points, grilles, readers) at the center
  of their back plate; ceiling props on the ceiling surface hanging down, long side along Z (drop-ceiling items reach
  down from the tile face, 11.2 mm); desk-top props on the desk top (0.75 m); doors on the opening pose.
- One `BoxCollider` per prop (none if out of reach), never mesh colliders.
- Smooth parts are `GrayboxMeshBuilder` meshes (lathe, tube, extrusion, strip), one mesh per material, saved as
  `Art/Meshes/Details/<prefab root>_<part>.asset` (part names must be unique in a prefab). Others are ProBuilder boxes.
- Materials are shared `Art/Materials/M_<SurfaceMaterial>.mat` (URP Lit; `RackGlow` URP Unlit additive). All in-world
  text and stickers are Czech; the exit sign says "EXIT".

## Room structure

| Prop | Model | Used |
|---|---|---|
| `DoorFrame` [cs] | casing round a 2.4 x 2.6 m opening on the room side, 0.1 m wide, 4 cm deep; does not enter the opening | every shell wall, hall door wall |
| `AutoSlidingDoor` [Doors] | bi-parting glass sliding door for a room opening; operator box, photo cells and RFID reader (55 x 125 x 20 mm, lock LED strip green/red) on both sides, exit sign on the owner's side only; Runtime `AutoSlidingDoor` | every connected room opening (owner side), hall door walls |
| `AisleDoor` [Doors] | single black-framed glass leaf, aisle width + 2 cm, slides to local +X, jamb right, header flush with containment roof, no floor rail, reader on the right rack end; Runtime `AisleDoor` | both ends of every hall pod |
| `PassengerLift` [Doors] | evacuation lift: 1.1 x 2.1 m two-panel centre-opening door (landing + car panels move together, photo cells into the car), car 1.6 x 1.5 x 2.3 m for 6-8 people (mirror, handrails, lit ceiling, operating panel), landing call buttons, floor indicator, exit sign over the door, evacuation-lift sign | lift end |
| `FreightLift` [Storage] | goods-lift landing, passenger lift design in galvanised steel: 2.0 x 2.2 m chamfered two-panel door, frame, sill, call buttons, floor indicator; never opens, one collider | storage room east wall |

## Lighting

| Prop | Model | Used |
|---|---|---|
| `LedPanel600` [Detail] | lay-in panel 0.6 x 0.6, white 18 mm frame, opal diffuser; spot down, no shadows | offices, security office, corridors |
| `LedPanel1200` [Detail] | same, 0.6 x 1.2 | **unused** |
| `LedBatten` [Storage] | surface tri-proof batten 1.5 m (along Z): white tray, curved opal diffuser, clips, end caps | power room, cooling plant, storage room |
| `PendantLight` [Hall] | `LedBatten` hanging 1.9 m below the deck on two wire ropes (ceiling plates, cups, ferrules, thimbles), black power cable from a roof junction box | hall (8 per tile) |
| `AisleLight` [Doors] | LED strip 1.2 m under the containment roof | hall pods (2 each) |
| `ExitSign` [Hall] | 360 x 160 x 50 mm white housing, emissive green "EXIT" face (`T_ExitSign`) | on `AutoSlidingDoor`, `PassengerLift` |

## Safety and security

| Prop | Model | Used |
|---|---|---|
| `Sprinkler` [Detail] | pendent glass-bulb sprinkler 63 mm: chrome escutcheon, hex body, brass ring, red bulb, frame arms, 16-tine deflector | corridors, small rooms |
| `SmokeDetector` [Detail] | 100 x 46 mm glossy white, status lens, mesh chamber, domed cap | corridors, small rooms, hall deck |
| `CeilingSpeaker` [Detail] | recessed PA speaker 216 mm, white trim, domed grille 185 mm (`T_SpeakerGrille`) | corridors, offices (not the hall, not technical rooms) |
| `FireExtinguisher` [Safety] | 6 kg ABC powder, Ø160 mm, ~0.6 m on its bracket, label `T_ExtinguisherLabel`, chrome head with gauge, hose to a nozzle | corridor fire arm, every small room |
| `FireCallPoint` [Safety] | 92 x 92 x 52 mm red manual call point, push symbol, fire pictogram, at 1.4 m | corridor fire arm, small rooms, hall walls |
| `HydrantCabinet` [Safety] | 0.65 x 0.65 x 0.24 red hose cabinet, window with gasket, white hose on a red reel, bottom at 0.9 m | corridor fire arm |
| `GasNozzle` [cs] | 360° inert-gas nozzle | hall gas lines (2 per pod) |
| `LightSwitch` [Detail] | 80 x 80 mm plate, 56 mm rocker, 11 mm proud, centre at 1.05 m | corridors, small rooms, hall door walls |
| `SecurityDome` [Detail] | ceiling dome camera 111 x 80 mm, clear dome over a black inner dome; feed 30° down; Runtime `SecurityCamera` | corridor crossings/T/corners, small rooms |
| `SecurityWallDome` [Detail] | same housing on a wall, feed 25° down | **unused** (hall walls carry no cameras) |
| `SecurityPendant` [Detail] | junction box clamped to a beam, pole with cable, dome at its end (dome at 3.0 m) | hall girders (2 per open east edge) |

## Building services

| Prop | Model | Used |
|---|---|---|
| `PowerSocket` [Detail] | flush 230 V type E on 80 x 80 plate, 10 mm proud | offices, security office, corridor arm 1, kitchenette, printer |
| `DataSocket` [Detail] | flush double RJ45 on 80 x 80 plate, 8 mm proud | corridor arm 1, terminal outlets, security racks |
| `FloorBox` [Hall] | 150 x 150 mm floor box, pewter plate, hinged lid, screw lock; under the back of the desk it feeds | security office consoles |
| `WallVentGrille` [Detail] | 600 x 300 louvred grille under the ceiling | technical rooms back wall (2) |
| `FloorVentGrille` [Detail] | 600 x 600 louvred grille 0.15 m above the floor | one per corridor cell |
| `CeilingDiffuser` [Safety] | 600 mm four-way diffuser, nested square cones | offices, security office |
| `FloorVentTile` [cs] | 0.6 x 0.6 perforated raised-floor tile (25 mm hole pitch texture), 12 mm proud | hall cold aisles |
| `ElectricalPanel` [cs] | wall distribution board 0.6 x 0.8 x 0.22, trunking up to the ceiling | **unused** |
| `PipeGauge` [Plant] | dial thermometer on a brass stem, dial +Z | cooling-plant pumps |

Non-prop services (built inside rooms by `RoomInfrastructure` / `RoomPipes` / `InfrastructureModels`): pipe risers with
strap clamps, rosettes and ball valves (lever grip = pipe colour: red fire, green water, yellow gas); insulated wide pipes
(aluminium, coloured bands); trunking 120 x 60 mm with mitred corners; cable trays (galvanised, slotted, cables, M10
rods, strut trapeze); rectangular ducts (aluminium/stainless, flanges, hangers), bellows, collar frames, duct grilles;
painted floor lines and hatching; conduits (painted tube, chrome rosette).

## Office and security furniture

| Prop | Model | Used |
|---|---|---|
| `Desk` [cs] | white panel desk 1.6 x 0.75 x 0.8, 25 mm top, side panels, modesty panel | small office (1), security office (2) |
| `OfficeChair` [Chair] | black swivel chair, five-star base, quilted lower backrest (`T_ChairQuilt`), loop armrests; 0.58 wide, seat 0.47, 1.05 tall | at every desk and meeting table |
| `Workstation` [cs] | one monitor, keyboard, mouse right of it; desk-top | variant of `PlaceDesk` (not placed now) |
| `DualWorkstation` [Office] | two 24" monitors 0.55 m apart angled in, keyboard (~100 keys, numpad), mouse; monitor = slim body, lit picture `T_MonitorScreen`, neck, silver base | every desk |
| `PcTower` [Devices] | mid-tower 0.2 x 0.45 x 0.45, Dell OptiPlex-like front, diamond mesh `T_PcVent` | beside every desk |
| `MeetingTable` [cs] | 1.8 x 0.75 x 0.9, light 30 mm top on two T-legs | small office |
| `FilingCabinet` [cs] | five-drawer 0.45 x 1.32 x 0.6, white steel, finger pockets | small office (2) |
| `Printer` [Office] | A3 copier on a 4-drawer cabinet 0.6 x 1.27 x 0.65, scanner with feeder, control panel | small office |
| `WallTv` [Office] | 65" 1.46 x 0.84 x 0.028, centre at 1.6 m | small office back wall |
| `VideoScreen` [Hall] | 55" display on a wall bracket; screen quad for a camera feed (`SecurityMonitor`) | security office video wall (8) |
| `CoffeeCounter` [Office] | kitchenette 1.8 m: white gloss base units (3 x 0.6), stone top at 0.9, dark double sink, gooseneck tap, wall cabinets | small office |
| `CoffeeMachine` [Office] | bean-to-cup 0.19 x 0.34 x 0.48, graphite, copper band, espresso cup | on the counter |
| `Fridge` [Office] | fridge-freezer 0.6 x 1.8 x 0.65, white, silver D-handles | small office |
| `Mug` [Office] | white ceramic mug 83 mm, triangular handle | counter, desks, meeting table |
| `PaperStack` [Office] | 16 loose A4 sheets, top sheet `T_PaperPage` | desks, cabinets, meeting table, security "ShiftLog" |
| `PhotoFrame` [Office] | 15 x 20 cm black mitred frame, mount, `T_PhotoPicture` behind glass, hinged leg | small office desk |
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
| `SwitchgearCabinet` [Power] | LV switchgear 0.8 x 2.2 x 0.6: instrument door with control panel, 3 breaker doors (molded-case breaker, rotary knob), busbar duct up; no meters | power room (2) |
| `EquipmentCabinet` [Power] | UPS 0.6 x 2.0 x 0.8 graphite: two vents, control panel, tag, green status strip, handle | power room (2) |
| `AnalogMeterPanel` [Power] | 6 large kW dials (`T_PowerDial`) in 2 x 3 on a rounded board, trunking to the ceiling; clue display (`AnalogGauge` planned) | power room back wall |

## Cooling plant

| Prop | Model | Used |
|---|---|---|
| `PlateHeatExchanger` [Plant] | 2.2 x 2.0 x 0.9 aluminium-clad, diamond-folded front, control panel, blue channel skid | cooling plant east |
| `PlateHeatExchangerMirrored` [Plant] | same, secondary pipes at the other end | cooling plant west |
| `CirculationPump` [Plant] | end-suction pump 1.0 x 0.7 x 0.5: cast-iron volute, blue ribbed motor, tapered feet, skid; pivot = discharge axis | cooling plant (2) |
| `BufferTank` [Plant] | Ø0.7 x 2.2 on three legs, aluminium clad, green bands, two top nozzles | cooling plant |
| `AirHandlerUnit` [Plant] | CRAH unit with duct to the ceiling and a damper | cooling plant |
| `ExpansionVessel` [Plant] | Ø0.44 x 1.1 white vessel on three legs, blue cap, yellow sticker, side nozzle | cooling plant |
| `CoolingMeterPanel` [Plant] | 6 narrow pipes from the floor with 3x life-size dial thermometers (`T_ThermometerDial`), into the exchangers' end faces; clue display | cooling plant back wall |
| `AirHandlingUnit` [Technical] | AHU 3.0 x 2.2 x 1.2: filter/coil/fan sections, green flow/return with valves and `PipeGauge`s, supply duct | **unused** |

Stickers and plates are quads 3 mm off the surface showing cells of `T_PlantLabels.png` (4 x 4 Czech atlas: plant
labels, transformer plate, switchgear tag, UPS tag; material `PlantLabels`).

## Server hall

| Prop | Model | Used |
|---|---|---|
| `Rack` [Racks] | 42 U rack 0.6 x 2.0 x 1.2, only the front modelled; Runtime `Rack.Configure(openable, seed)`. Closed: fixed door, flush handle, no interaction. Openable: pulled-out lever handle, door ajar, `RackDoor` (F opens fully / shuts), `RackGlow` (faint breathing white-beige outline quad `DoorGlow`, `T_RackGlow`, lit only while the door is shut perfectly). Door: hex grille `T_RackGrille` (alpha, mipmapped, anisotropic) | every hall rack slot and the 2 security-office slots (placed by `RackPlacement`, not in prefabs) |
| `HallSignCube` [Hall] | white cube on two rods (bottom at 4.2 m), the tile code on all four faces (`HallSign.Code`) | one per hall tile |

**Rack fillings:** 12 deterministic random 42 U fillings (`ContentVariants`) as shared meshes in `Art/Meshes/Racks/`,
listed in `Data/RackContents.asset` (`RackContentLibrary`); a rack picks one by its seed. Contents: Dell R740xd 2 U,
HP DL380p Gen8 2 U, Inter-Tech 3 U chassis, rack UPS (bottom only), 1 U PDU (lower half only), 1 U blanking panels;
never switches, routers or cables; the top unit is always a server; status LEDs show through the grille.

## Placement rules

| Thing | Only in |
|---|---|
| shelving, cardboard boxes | storage room |
| lockers, locker bench | security office |
| transformers, switchgear, UPS | power room |
| heat exchangers, pumps, tanks, air handler, expansion vessel | cooling plant |
| racks | hall pods, security office (2) |
| coffee corner, fridge, TV, meeting table, printer, filing cabinets | small office |
| on desks | monitors, keyboard, mouse, papers, mugs, photo — never boxes, binders or laptops |
| never anywhere | service carts, laptops, cable drums, binders, emergency lights, decorative/hinged doors, roller shutters |

## Textures (`Art/Textures`)

Generated by `GrayboxMaterials` (rewritten by every Create Preview Scene): `T_PcVent`, `T_ExtinguisherLabel`,
`T_SpeakerGrille`, `T_RackGrille`, `T_TraySlots`, `T_RackGlow`, `T_CeilingTile600`, `T_FloorTilePerforated`,
`T_ServerSlots`, `T_ServerHoneycomb`. Drawn files (edit the PNG): `T_MonitorScreen`, `T_ChairQuilt`, `T_PaperPage`,
`T_ExitSign`, `T_PhotoPicture`, `T_ThermometerDial`, `T_PowerDial`, `T_PlantLabels`. `T_RackPerforation` is unused.

## Surface materials (`SurfaceMaterial`, append only; agree on new slots with the other developer)

Floor FloorVent Wall Ceiling Trim Metal Furniture Upholstery Screen LightPanel Whiteboard SafetyRed CeilingGrid Trunking
PowerCable DataCable DataTray PipeWater PipeGas HallFloor HallWall Steel RoofDeck SafetyYellow SafetyGreen Glass SignFace
WireMesh LedRed Chrome Brass BulbRed BulbClear BlackHousing DomeClear LensGlass SpeakerWhite SpeakerGrille DetectorWhite
DetectorMesh CeilingTile ReaderBlack ExtinguisherLabel HoseRed HoseWhite KeyboardGraphite KeyboardKey MouseBlack PcVent
MonitorImage ChairLeather ChairQuilt PaperPage ExitSignFace FloorBoxMetal StainlessSteel OfficeTopLight OfficeLegWhite
DeskWhite StoneTop CopperTrim CoffeeGraphite CoffeePanel CabinetWhite SmokedTank CabinetPull LockerBody LockerDoor
LockerPlate BenchWood BenchFrame PhotoPicture ValveNickel ValveSteel ValveGrip PipeClamp AluminiumClad MotorGreen
SupportBlue DialFace PlantLabels CastIron CabinetGrey GalvanisedSteel ShelfBoard Cardboard CardboardDark CardboardEdge
ElectricGrey UpsGraphite ResinCoil PowerDialFace RackBlack RackMesh ServerBlack ServerSilver DriveBay LatchOrange LatchRed
LedAmber ServerSlots ServerHoneycomb TrayPerforated FloorTilePerforated Concrete DuctStainless ConduitViolet
ConduitMagenta RackHandleLight CeilingTile600 RackGlow ConduitBrown ConduitGreen
