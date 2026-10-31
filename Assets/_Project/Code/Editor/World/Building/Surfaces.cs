using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The surface kinds rooms and props are built from. Each one is a single shared material asset, so
    /// final textures (task 4.6) and datacenter profiles (task 4.4) change one material, not every mesh.
    /// Only append new values; the names are part of the asset paths.
    /// </summary>
    public enum SurfaceMaterial
    {
        Floor,
        FloorVent,
        Wall,
        Ceiling,
        Trim,
        Metal,
        Furniture,
        Upholstery,
        Screen,
        LightPanel,
        MugCeramic,
        SafetyRed,
        CeilingGrid,
        Trunking,
        PowerCable,
        DataCable,
        DataTray,
        PipeWater,
        PipeGas,
        HallFloor,
        HallWall,
        Steel,
        RoofDeck,
        SafetyYellow,
        SafetyGreen,
        Glass,
        SignFace,
        WireMesh,
        LedRed,
        // Detail props (sprinkler, camera dome, ceiling speaker), appended with the photo-realistic rebuild.
        Chrome,
        Brass,
        BulbRed,
        BulbClear,
        BlackHousing,
        DomeClear,
        LensGlass,
        SpeakerWhite,
        SpeakerGrille,
        DetectorWhite,
        DetectorMesh,
        CeilingTile,
        ReaderBlack,
        ExtinguisherLabel,
        HoseRed,
        HoseWhite,
        KeyboardGraphite,
        KeyboardKey,
        MouseBlack,
        PcVent,
        MonitorImage,
        ChairLeather,
        ChairQuilt,
        PaperPage,
        ExitSignFace,
        FloorBoxMetal,
        StainlessSteel,
        OfficeTopLight,
        OfficeLegWhite,
        DeskWhite,
        StoneTop,
        CopperTrim,
        CoffeeGraphite,
        CoffeePanel,
        CabinetWhite,
        SmokedTank,
        CabinetPull,
        // Changing-room furniture: light grey locker carcass, slate-blue doors and name plates, oak bench slats, cream bench frame.
        LockerBody,
        LockerDoor,
        LockerPlate,
        BenchWood,
        BenchFrame,
        PhotoPicture,
        ValveNickel,
        ValveSteel,
        ValveGrip,
        PipeClamp,
        // Plant rooms (product photos): aluminium-clad insulation, green motors, blue pipe supports, the thermometer dial face (texture).
        AluminiumClad,
        MotorGreen,
        SupportBlue,
        DialFace,
        PlantLabels,
        CastIron,
        CabinetGrey,
        // Storage room (product photos): galvanised sheet steel of the shelving and the goods lift, chipboard shelf boards, kraft cardboard
        // of the boxes, its darker handle holes and seams and the lighter corrugated cut edge round a hole.
        GalvanisedSteel,
        ShelfBoard,
        Cardboard,
        CardboardDark,
        CardboardEdge,
        // Power room (product photos): light grey sheet steel of the switchgear and transformer enclosure, graphite UPS cabinet, orange-brown cast-resin
        // coils, the kW dial face (texture).
        ElectricGrey,
        UpsGraphite,
        ResinCoil,
        PowerDialFace,
        // Server racks and the servers in them (product photos): painted black cabinet, hex-perforated door (alpha-cut texture), black and
        // silver server bezels, dark drive carriers, red status windows, amber status LED, honeycomb and slotted vent textures.
        RackBlack,
        RackMesh,
        ServerBlack,
        ServerSilver,
        DriveBay,
        LatchRed,
        LedAmber,
        ServerHoneycomb,
        // Rack items: the 3 U chassis' slotted drive-bay covers (texture), the black void behind drive carriers and inside ports, the rack UPS' blue LCD (texture), the dark grey switch faceplate.
        ChassisVent,
        ServerVoid,
        UpsDisplay,
        SwitchFace,
        // Hall: slotted galvanised cable tray (alpha-cut slots), perforated raised-floor tile (round holes).
        TrayPerforated,
        FloorTilePerforated,
        // Hall: cast concrete ceiling; painted HVAC ducts (cold supply light blue, warm return orange), distinct from the gas, water and fire pipe colours.
        Concrete,
        // HVAC ducts: brushed stainless like a passenger lift (not a pipe colour), cable drops in violet (power) and magenta (data), the pulled-out rack handle's light inside, the perforated concrete ceiling tile.
        DuctStainless,
        ConduitViolet,
        ConduitMagenta,
        RackHandleLight,
        CeilingTile600,
        // The faint glow of an openable rack's door: an unlit, additive, soft-edged white-beige quad.
        RackGlow,
        // Cable drops into the aisles: brown (power) and green (data).
        ConduitBrown,
        ConduitGreen,
        // Passenger lift: brushed stainless ceiling panels (texture T_LiftCeiling).
        LiftCeiling,
    }

    /// <summary>
    /// Creates the shared surface materials in <c>Art/Materials</c> as plain URP Lit greys (plus the
    /// colours real installations use: red fire water, green water, yellow gas, yellow data trays).
    /// Existing materials are never touched, except by an explicit rebuild.
    /// </summary>
    internal static class Surfaces
    {
        public const string Folder = "Assets/_Project/Art/Materials";

        private readonly struct Look
        {
            public readonly Color Color;
            public readonly float Metallic;
            public readonly float Smoothness;
            public readonly Color Emission;

            /// <summary>Below 1 the material is transparent (alpha blended).</summary>
            public float Alpha => Color.a;

            public Look(float grey, float metallic, float smoothness) : this(new Color(grey, grey, grey), metallic, smoothness) { }

            public Look(Color color, float metallic, float smoothness, Color emission = default)
            {
                Color = color;
                Metallic = metallic;
                Smoothness = smoothness;
                Emission = emission;
            }
        }

        // Different grey values keep surfaces apart; final textures go into these materials (task 4.6).
        private static readonly Dictionary<SurfaceMaterial, Look> Looks = new Dictionary<SurfaceMaterial, Look>
        {
            { SurfaceMaterial.Floor, new Look(0.42f, 0f, 0.35f) },
            { SurfaceMaterial.FloorVent, new Look(0.28f, 0.5f, 0.4f) },
            { SurfaceMaterial.Wall, new Look(0.72f, 0f, 0.2f) },
            { SurfaceMaterial.Ceiling, new Look(0.82f, 0f, 0.05f) },
            { SurfaceMaterial.Trim, new Look(0.5f, 0.3f, 0.4f) },
            { SurfaceMaterial.Metal, new Look(0.55f, 0.8f, 0.5f) },
            { SurfaceMaterial.Furniture, new Look(0.62f, 0f, 0.3f) },
            { SurfaceMaterial.Upholstery, new Look(0.18f, 0f, 0.1f) },
            { SurfaceMaterial.Screen, new Look(0.04f, 0f, 0.85f) },
            // Fluorescent tubes: emissive so the fitting reads as lit; the light itself is a Light component.
            { SurfaceMaterial.LightPanel, new Look(Color.white, 0f, 0.5f, new Color(1f, 0.96f, 0.9f) * 1.5f) },
            { SurfaceMaterial.MugCeramic, new Look(0.9f, 0f, 0.7f) },
            { SurfaceMaterial.SafetyRed, new Look(new Color(0.7f, 0.08f, 0.06f), 0.1f, 0.6f) },
            { SurfaceMaterial.CeilingGrid, new Look(new Color(0.94f, 0.94f, 0.93f), 0.05f, 0.5f) },
            { SurfaceMaterial.Trunking, new Look(0.93f, 0f, 0.35f) },
            { SurfaceMaterial.PowerCable, new Look(0.06f, 0f, 0.3f) },
            { SurfaceMaterial.DataCable, new Look(new Color(0.12f, 0.3f, 0.75f), 0f, 0.4f) },
            { SurfaceMaterial.DataTray, new Look(new Color(0.9f, 0.72f, 0.1f), 0.3f, 0.5f) },
            { SurfaceMaterial.PipeWater, new Look(new Color(0.15f, 0.5f, 0.25f), 0.2f, 0.6f) },
            { SurfaceMaterial.PipeGas, new Look(new Color(0.92f, 0.78f, 0.1f), 0.2f, 0.6f) },
            // Server hall: sealed epoxy floor, painted block walls, grey steel structure, metal roof deck.
            { SurfaceMaterial.HallFloor, new Look(new Color(0.36f, 0.38f, 0.4f), 0f, 0.55f) },
            { SurfaceMaterial.HallWall, new Look(new Color(0.78f, 0.79f, 0.8f), 0f, 0.15f) },
            { SurfaceMaterial.Steel, new Look(new Color(0.3f, 0.33f, 0.37f), 0.6f, 0.45f) },
            { SurfaceMaterial.RoofDeck, new Look(0.5f, 0.5f, 0.3f) },
            { SurfaceMaterial.SafetyYellow, new Look(new Color(0.95f, 0.75f, 0.05f), 0f, 0.4f) },
            { SurfaceMaterial.SafetyGreen, new Look(new Color(0.1f, 0.6f, 0.25f), 0f, 0.5f, new Color(0.1f, 0.8f, 0.3f) * 1.2f) },
            { SurfaceMaterial.Glass, new Look(new Color(0.6f, 0.7f, 0.75f, 0.25f), 0f, 0.9f) },
            // Sign faces glow a little so the codes stay readable from far away.
            { SurfaceMaterial.SignFace, new Look(new Color(0.95f, 0.95f, 0.95f), 0f, 0.3f, new Color(0.35f, 0.35f, 0.35f)) },
            // Steel wire mesh of the cage partitions in the hall: see-through, so the hall stays readable.
            { SurfaceMaterial.WireMesh, new Look(new Color(0.35f, 0.37f, 0.4f, 0.45f), 0.6f, 0.4f) },
            // Red status LED (locked access reader); the green one is SafetyGreen.
            { SurfaceMaterial.LedRed, new Look(new Color(0.8f, 0.05f, 0.04f), 0f, 0.5f, new Color(1f, 0.08f, 0.05f) * 1.5f) },
            // Photo-realistic detail props: polished chrome, brass, the red glycerine bulb and its clear seal, satin black
            // camera housing, the clear dome, the lens, the white trim of the ceiling speaker and its grille.
            { SurfaceMaterial.Chrome, new Look(new Color(0.86f, 0.88f, 0.9f), 0.9f, 0.9f) },
            { SurfaceMaterial.Brass, new Look(new Color(0.76f, 0.56f, 0.24f), 0.85f, 0.6f) },
            { SurfaceMaterial.BulbRed, new Look(new Color(0.78f, 0.03f, 0.02f), 0f, 0.95f, new Color(0.25f, 0.0f, 0.0f)) },
            { SurfaceMaterial.BulbClear, new Look(new Color(0.92f, 0.92f, 0.9f), 0f, 0.95f) },
            { SurfaceMaterial.BlackHousing, new Look(new Color(0.03f, 0.03f, 0.034f), 0.1f, 0.5f) },
            { SurfaceMaterial.DomeClear, new Look(new Color(0.8f, 0.85f, 0.88f, 0.1f), 0f, 0.97f) },
            { SurfaceMaterial.LensGlass, new Look(new Color(0.01f, 0.012f, 0.02f), 0.3f, 1f) },
            { SurfaceMaterial.SpeakerWhite, new Look(new Color(0.9f, 0.9f, 0.88f), 0f, 0.4f) },
            { SurfaceMaterial.SpeakerGrille, new Look(new Color(0.76f, 0.77f, 0.76f), 0.2f, 0.3f) },
            // Smoke detector: glossy white ABS and its dark insect mesh; drop ceiling: matt white mineral-fibre tiles.
            { SurfaceMaterial.DetectorWhite, new Look(new Color(0.93f, 0.93f, 0.92f), 0f, 0.6f) },
            { SurfaceMaterial.DetectorMesh, new Look(new Color(0.02f, 0.02f, 0.022f), 0.5f, 0.25f) },
            { SurfaceMaterial.ReaderBlack, new Look(new Color(0.04f, 0.04f, 0.045f), 0.1f, 0.3f) },
            { SurfaceMaterial.ExtinguisherLabel, new Look(Color.white, 0f, 0.45f) },
            { SurfaceMaterial.HoseRed, new Look(new Color(0.78f, 0.16f, 0.07f), 0f, 0.35f) },
            { SurfaceMaterial.HoseWhite, new Look(new Color(0.9f, 0.9f, 0.85f), 0f, 0.12f) },
            { SurfaceMaterial.KeyboardGraphite, new Look(new Color(0.1f, 0.1f, 0.11f), 0.05f, 0.3f) },
            { SurfaceMaterial.KeyboardKey, new Look(new Color(0.16f, 0.16f, 0.17f), 0f, 0.35f) },
            { SurfaceMaterial.MouseBlack, new Look(new Color(0.045f, 0.045f, 0.05f), 0f, 0.3f) },
            { SurfaceMaterial.CeilingTile, new Look(new Color(0.95f, 0.95f, 0.94f), 0f, 0.04f) },
            // PC case: the diamond-perforated front (texture), and the lit desktop picture of the monitors (texture + emission).
            { SurfaceMaterial.PcVent, new Look(Color.white, 0.2f, 0.3f) },
            { SurfaceMaterial.MonitorImage, new Look(Color.white, 0f, 0.55f, new Color(0.7f, 0.7f, 0.7f)) },
            // Office chair: black leatherette pads and the quilted black fabric (texture) of the lower backrest and seat.
            { SurfaceMaterial.ChairLeather, new Look(new Color(0.035f, 0.035f, 0.038f), 0f, 0.5f) },
            { SurfaceMaterial.ChairQuilt, new Look(Color.white, 0f, 0.2f) },
            // Top sheet of a paper stack (A4 page texture), lit exit sign face (texture + emission), hammered pewter floor box lid (texture).
            { SurfaceMaterial.PaperPage, new Look(Color.white, 0f, 0.15f) },
            { SurfaceMaterial.ExitSignFace, new Look(Color.white, 0f, 0.4f, new Color(0.8f, 0.8f, 0.8f)) },
            // Matt stainless steel of the floor box lid (no texture), polished-brushed stainless of the lift, light office desk surfaces.
            { SurfaceMaterial.FloorBoxMetal, new Look(new Color(0.30f, 0.31f, 0.32f), 0.85f, 0.35f) },
            { SurfaceMaterial.StainlessSteel, new Look(new Color(0.30f, 0.31f, 0.32f), 0.85f, 0.35f) },
            { SurfaceMaterial.OfficeTopLight, new Look(new Color(0.83f, 0.76f, 0.63f), 0f, 0.3f) },
            { SurfaceMaterial.OfficeLegWhite, new Look(new Color(0.86f, 0.87f, 0.87f), 0.2f, 0.4f) },
            { SurfaceMaterial.DeskWhite, new Look(new Color(0.9f, 0.9f, 0.89f), 0f, 0.3f) },
            // Kitchen: light stone worktop, the copper trim and the graphite plastic of the coffee machine.
            { SurfaceMaterial.StoneTop, new Look(new Color(0.8f, 0.8f, 0.78f), 0f, 0.55f) },
            { SurfaceMaterial.CopperTrim, new Look(new Color(0.72f, 0.5f, 0.38f), 0.8f, 0.45f) },
            { SurfaceMaterial.CoffeeGraphite, new Look(new Color(0.14f, 0.13f, 0.13f), 0f, 0.25f) },
            // The lighter control panel of the coffee machine and the cool white sheet steel of the filing cabinets.
            { SurfaceMaterial.CoffeePanel, new Look(new Color(0.36f, 0.35f, 0.34f), 0f, 0.3f) },
            { SurfaceMaterial.SmokedTank, new Look(new Color(0.22f, 0.22f, 0.24f, 0.9f), 0f, 0.8f) },
            { SurfaceMaterial.CabinetPull, new Look(new Color(0.34f, 0.35f, 0.37f), 0.2f, 0.4f) },
            { SurfaceMaterial.CabinetWhite, new Look(new Color(0.83f, 0.85f, 0.88f), 0.1f, 0.45f) },
            { SurfaceMaterial.LockerBody, new Look(new Color(0.8f, 0.81f, 0.82f), 0.1f, 0.4f) },
            { SurfaceMaterial.LockerDoor, new Look(new Color(0.3f, 0.4f, 0.43f), 0.1f, 0.45f) },
            { SurfaceMaterial.LockerPlate, new Look(new Color(0.21f, 0.28f, 0.3f), 0.1f, 0.4f) },
            { SurfaceMaterial.BenchWood, new Look(new Color(0.8f, 0.6f, 0.38f), 0f, 0.4f) },
            { SurfaceMaterial.ValveNickel, new Look(new Color(0.8f, 0.78f, 0.72f), 0.9f, 0.6f) },
            { SurfaceMaterial.ValveSteel, new Look(new Color(0.78f, 0.8f, 0.82f), 0.85f, 0.5f) },
            { SurfaceMaterial.ValveGrip, new Look(new Color(0.1f, 0.11f, 0.125f), 0f, 0.55f) },
            { SurfaceMaterial.PipeClamp, new Look(new Color(0.62f, 0.64f, 0.66f), 0.7f, 0.45f) },
            { SurfaceMaterial.AluminiumClad, new Look(new Color(0.78f, 0.8f, 0.82f), 0.85f, 0.5f) },
            { SurfaceMaterial.MotorGreen, new Look(new Color(0.1f, 0.45f, 0.36f), 0.3f, 0.5f) },
            { SurfaceMaterial.SupportBlue, new Look(new Color(0.1f, 0.3f, 0.7f), 0.2f, 0.45f) },
            { SurfaceMaterial.PlantLabels, new Look(Color.white, 0f, 0.35f) },
            { SurfaceMaterial.CastIron, new Look(new Color(0.3f, 0.32f, 0.34f), 0.5f, 0.35f) },
            { SurfaceMaterial.CabinetGrey, new Look(new Color(0.52f, 0.54f, 0.56f), 0.2f, 0.35f) },
            { SurfaceMaterial.DialFace, new Look(Color.white, 0f, 0.4f) },
            { SurfaceMaterial.PhotoPicture, new Look(Color.white, 0f, 0.35f) },
            { SurfaceMaterial.BenchFrame, new Look(new Color(0.9f, 0.9f, 0.86f), 0.1f, 0.4f) },
            { SurfaceMaterial.GalvanisedSteel, new Look(new Color(0.66f, 0.68f, 0.7f), 0.85f, 0.45f) },
            { SurfaceMaterial.ShelfBoard, new Look(new Color(0.78f, 0.6f, 0.36f), 0f, 0.15f) },
            { SurfaceMaterial.Cardboard, new Look(new Color(0.74f, 0.58f, 0.38f), 0f, 0.1f) },
            { SurfaceMaterial.CardboardDark, new Look(new Color(0.1f, 0.075f, 0.05f), 0f, 0.1f) },
            { SurfaceMaterial.ElectricGrey, new Look(new Color(0.72f, 0.74f, 0.74f), 0.3f, 0.45f) },
            { SurfaceMaterial.UpsGraphite, new Look(new Color(0.12f, 0.13f, 0.14f), 0.3f, 0.4f) },
            { SurfaceMaterial.ResinCoil, new Look(new Color(0.55f, 0.28f, 0.08f), 0f, 0.55f) },
            { SurfaceMaterial.PowerDialFace, new Look(Color.white, 0f, 0.4f) },
            { SurfaceMaterial.CardboardEdge, new Look(new Color(0.55f, 0.4f, 0.24f), 0f, 0.1f) },
            { SurfaceMaterial.RackBlack, new Look(new Color(0.035f, 0.036f, 0.04f), 0.4f, 0.45f) },
            // The rack door's grille is a semi-transparent texture (alpha blended; coarse holes, so it stays calm from every angle).
            { SurfaceMaterial.RackMesh, new Look(new Color(0.8f, 0.8f, 0.82f, 0.999f), 0.5f, 0.4f) },
            { SurfaceMaterial.ServerBlack, new Look(new Color(0.025f, 0.025f, 0.028f), 0.2f, 0.35f) },
            { SurfaceMaterial.ServerSilver, new Look(new Color(0.62f, 0.63f, 0.64f), 0.85f, 0.45f) },
            { SurfaceMaterial.DriveBay, new Look(new Color(0.07f, 0.07f, 0.075f), 0.3f, 0.4f) },
            { SurfaceMaterial.LatchRed, new Look(new Color(0.62f, 0.04f, 0.09f), 0.1f, 0.5f) },
            { SurfaceMaterial.LedAmber, new Look(new Color(1f, 0.55f, 0.05f), 0f, 0.5f, new Color(1f, 0.5f, 0.05f) * 2f) },
            { SurfaceMaterial.ServerHoneycomb, new Look(Color.white, 0.2f, 0.35f) },
            { SurfaceMaterial.ChassisVent, new Look(Color.white, 0.3f, 0.4f) },
            { SurfaceMaterial.ServerVoid, new Look(new Color(0.003f, 0.003f, 0.004f), 0f, 0.15f) },
            { SurfaceMaterial.UpsDisplay, new Look(Color.white, 0f, 0.35f, new Color(0.15f, 0.25f, 0.7f) * 0.8f) },
            { SurfaceMaterial.SwitchFace, new Look(new Color(0.17f, 0.175f, 0.18f), 0.6f, 0.4f) },
            { SurfaceMaterial.TrayPerforated, new Look(new Color(0.66f, 0.68f, 0.7f), 0.85f, 0.5f) },
            { SurfaceMaterial.FloorTilePerforated, new Look(Color.white, 0.05f, 0.5f) },
            { SurfaceMaterial.Concrete, new Look(new Color(0.58f, 0.58f, 0.57f), 0f, 0.12f) },
            { SurfaceMaterial.DuctStainless, new Look(new Color(0.62f, 0.64f, 0.66f), 0.7f, 0.55f) },
            { SurfaceMaterial.ConduitViolet, new Look(new Color(0.55f, 0.28f, 0.8f), 0.1f, 0.5f) },
            { SurfaceMaterial.ConduitMagenta, new Look(new Color(0.88f, 0.22f, 0.58f), 0.1f, 0.5f) },
            { SurfaceMaterial.RackHandleLight, new Look(new Color(0.95f, 0.94f, 0.82f), 0f, 0.4f, new Color(0.45f, 0.44f, 0.3f)) },
            { SurfaceMaterial.CeilingTile600, new Look(Color.white, 0f, 0.1f) },
            { SurfaceMaterial.RackGlow, new Look(new Color(1f, 0.95f, 0.8f, 0.85f), 0f, 0f) },
            { SurfaceMaterial.ConduitBrown, new Look(new Color(0.45f, 0.27f, 0.12f), 0.1f, 0.5f) },
            { SurfaceMaterial.ConduitGreen, new Look(new Color(0.2f, 0.62f, 0.3f), 0.1f, 0.5f) },
            { SurfaceMaterial.LiftCeiling, new Look(Color.white, 0.85f, 0.35f) },
        };

        public static string PathOf(SurfaceMaterial surface) => $"{Folder}/M_{surface}.mat";

        /// <summary>Loads the material, creating it first if it does not exist yet.</summary>
        public static Material Get(SurfaceMaterial surface)
        {
            string path = PathOf(surface);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(surface == SurfaceMaterial.RackGlow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")) { name = $"M_{surface}" };
            ApplyLook(material, Looks[surface]);
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(material, path);
            if (surface == SurfaceMaterial.SpeakerGrille || surface == SurfaceMaterial.ExtinguisherLabel || surface == SurfaceMaterial.PcVent
                || surface == SurfaceMaterial.MonitorImage || surface == SurfaceMaterial.ChairQuilt || surface == SurfaceMaterial.PaperPage
                || surface == SurfaceMaterial.ExitSignFace || surface == SurfaceMaterial.PhotoPicture || surface == SurfaceMaterial.DialFace || surface == SurfaceMaterial.PlantLabels || surface == SurfaceMaterial.PowerDialFace
                || surface == SurfaceMaterial.CeilingTile600 || surface == SurfaceMaterial.RackGlow || surface == SurfaceMaterial.TrayPerforated || surface == SurfaceMaterial.FloorTilePerforated
                || surface == SurfaceMaterial.LiftCeiling || surface == SurfaceMaterial.RackMesh || surface == SurfaceMaterial.ServerHoneycomb
                || surface == SurfaceMaterial.ChassisVent || surface == SurfaceMaterial.UpsDisplay) ApplyTextures();
            return material;
        }

        public const string TextureFolder = "Assets/_Project/Art/Textures";

        /// <summary>
        /// Gives the speaker grille its perforation texture (a cheap stand-in for modelled holes): 16 x 16 round holes per
        /// 100 mm, matching the planar UVs of the detail meshes. Safe to repeat; hand-made materials are otherwise untouched.
        /// </summary>
        public static void ApplyTextures()
        {
            var material = Get(SurfaceMaterial.SpeakerGrille);
            material.SetTexture("_BaseMap", PerforationTexture());
            material.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(material);

            var label = Get(SurfaceMaterial.ExtinguisherLabel);
            label.SetTexture("_BaseMap", LabelTexture());
            EditorUtility.SetDirty(label);

            var vent = Get(SurfaceMaterial.PcVent);
            vent.SetTexture("_BaseMap", DiamondMeshTexture());
            vent.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(vent);

            var image = Get(SurfaceMaterial.MonitorImage);
            var picture = LoadTexture($"{TextureFolder}/T_MonitorScreen.png");
            image.SetTexture("_BaseMap", picture);
            image.SetTexture("_EmissionMap", picture);
            EditorUtility.SetDirty(image);

            var quilt = Get(SurfaceMaterial.ChairQuilt);
            quilt.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_ChairQuilt.png"));
            EditorUtility.SetDirty(quilt);

            var page = Get(SurfaceMaterial.PaperPage);
            page.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_PaperPage.png"));
            EditorUtility.SetDirty(page);

            var exit = Get(SurfaceMaterial.ExitSignFace);
            var exitSign = LoadTexture($"{TextureFolder}/T_ExitSign.png");
            exit.SetTexture("_BaseMap", exitSign);
            exit.SetTexture("_EmissionMap", exitSign);
            EditorUtility.SetDirty(exit);

            var photo = Get(SurfaceMaterial.PhotoPicture);
            photo.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_PhotoPicture.png"));
            EditorUtility.SetDirty(photo);

            var dial = Get(SurfaceMaterial.DialFace);
            dial.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_ThermometerDial.png"));
            EditorUtility.SetDirty(dial);

            var powerDial = Get(SurfaceMaterial.PowerDialFace);
            powerDial.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_PowerDial.png"));
            EditorUtility.SetDirty(powerDial);

            var labels = Get(SurfaceMaterial.PlantLabels);
            labels.SetTexture("_BaseMap", LoadTexture($"{TextureFolder}/T_PlantLabels.png"));
            EditorUtility.SetDirty(labels);

            // The rack door: a coarse semi-transparent hex grille, drawn on both sides (the inside shows when the door is open).
            var rackMesh = Get(SurfaceMaterial.RackMesh);
            rackMesh.SetTexture("_BaseMap", HexPerforationTexture());
            rackMesh.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(rackMesh);

            // The cable tray: slots cut out of the sheet (alpha clip), drawn on both sides.
            var tray = Get(SurfaceMaterial.TrayPerforated);
            tray.SetTexture("_BaseMap", TraySlotTexture());
            tray.SetFloat("_AlphaClip", 1f);
            tray.SetFloat("_Cutoff", 0.5f);
            tray.SetFloat("_Cull", 0f);
            tray.EnableKeyword("_ALPHATEST_ON");
            tray.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            EditorUtility.SetDirty(tray);

            // The rack glow: additive blending, both sides, no depth write.
            var glow = Get(SurfaceMaterial.RackGlow);
            glow.SetTexture("_BaseMap", RackGlowTexture());
            glow.SetFloat("_Surface", 1f);
            glow.SetFloat("_Blend", 2f);
            glow.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glow.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            glow.SetFloat("_ZWrite", 0f);
            glow.SetFloat("_Cull", 0f);
            glow.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(glow);

            var ceilingTile = Get(SurfaceMaterial.CeilingTile600);
            ceilingTile.SetTexture("_BaseMap", CeilingTileTexture());
            EditorUtility.SetDirty(ceilingTile);

            var liftCeiling = Get(SurfaceMaterial.LiftCeiling);
            liftCeiling.SetTexture("_BaseMap", LiftCeilingTexture());
            EditorUtility.SetDirty(liftCeiling);

            var floorTile = Get(SurfaceMaterial.FloorTilePerforated);
            floorTile.SetTexture("_BaseMap", FloorTileTexture());
            EditorUtility.SetDirty(floorTile);

            var honeycomb = Get(SurfaceMaterial.ServerHoneycomb);
            honeycomb.SetTexture("_BaseMap", HoneycombTexture());
            EditorUtility.SetDirty(honeycomb);

            var chassisVent = Get(SurfaceMaterial.ChassisVent);
            chassisVent.SetTexture("_BaseMap", ChassisVentTexture());
            EditorUtility.SetDirty(chassisVent);

            var upsDisplay = Get(SurfaceMaterial.UpsDisplay);
            upsDisplay.SetTexture("_BaseMap", UpsDisplayTexture());
            EditorUtility.SetDirty(upsDisplay);

            var lid = Get(SurfaceMaterial.FloorBoxMetal);
            lid.SetTexture("_BaseMap", null);
            EditorUtility.SetDirty(lid);
        }

        /// <summary>Loads a texture, importing the file first when Unity has not seen it yet (it may have been added from outside the Editor).</summary>
        private static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null && File.Exists(path))
            {
                AssetDatabase.ImportAsset(path);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return texture;
        }

        /// <summary>Paths of the generated textures already written again by the running <see cref="RegenerateTextures"/>; null outside it.</summary>
        private static HashSet<string> rewritten;

        /// <summary>
        /// Runs <paramref name="build"/> with every generated texture written again the first time it is asked for (in place, same
        /// GUID), so a change to a texture generator reaches the materials. Outside it an existing file is kept.
        /// </summary>
        public static void RegenerateTextures(System.Action build)
        {
            rewritten = new HashSet<string>();
            try
            {
                build();
            }
            finally
            {
                rewritten = null;
            }
        }

        /// <summary>The generated texture at <paramref name="path"/>, or null when it has to be written (missing, or due again).</summary>
        private static Texture2D ExistingTexture(string path)
        {
            if (rewritten != null && rewritten.Add(path)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// The diamond perforation of a PC case front: dark diamond holes in a black sheet, 2 x 2 holes per tile, matching the
        /// planar UVs of the PC prop (one tile is <c>Props.PcVentTile</c> metres).
        /// </summary>
        private static Texture2D DiamondMeshTexture()
        {
            string path = $"{TextureFolder}/T_PcVent.png";
            var existing = ExistingTexture(path);
            if (existing != null) return existing;

            const int size = 128;
            const float cell = size / 2f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + y + 1f) / cell;
                    float v = (x - y + size + 1f) / cell;
                    float du = Mathf.Abs(u - Mathf.Floor(u) - 0.5f);
                    float dv = Mathf.Abs(v - Mathf.Floor(v) - 0.5f);
                    bool hole = du < 0.36f && dv < 0.36f;
                    pixels[y * size + x] = hole ? new Color(0.01f, 0.01f, 0.012f) : new Color(0.1f, 0.1f, 0.11f);
                }
            }
            texture.SetPixels(pixels);
            Directory.CreateDirectory(TextureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// A generic extinguisher label (no real text): a red sheet with a white frame, a title bar, three rows of an icon box
        /// and two text bars, a row of three icon boxes and a white strip with a grey logo block at the foot.
        /// </summary>
        private static Texture2D LabelTexture()
        {
            string path = $"{TextureFolder}/T_ExtinguisherLabel.png";
            var existing = ExistingTexture(path);
            if (existing != null) return existing;

            const int width = 100;
            const int height = 190;
            var red = new Color(0.72f, 0.07f, 0.06f);
            var white = new Color(0.96f, 0.96f, 0.95f);
            var grey = new Color(0.55f, 0.55f, 0.55f);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = red;

            void Fill(int x, int y, int w, int h, Color color)
            {
                for (int yy = y; yy < y + h && yy < height; yy++)
                {
                    for (int xx = x; xx < x + w && xx < width; xx++)
                    {
                        if (xx >= 0 && yy >= 0) pixels[yy * width + xx] = color;
                    }
                }
            }

            Fill(3, 3, width - 6, 2, white);
            Fill(3, height - 5, width - 6, 2, white);
            Fill(3, 3, 2, height - 6, white);
            Fill(width - 5, 3, 2, height - 6, white);
            Fill(18, height - 22, 64, 9, white);
            Fill(30, height - 33, 40, 4, white);
            for (int row = 0; row < 3; row++)
            {
                int y = height - 62 - row * 24;
                Fill(10, y, 16, 16, white);
                Fill(13, y + 3, 10, 10, grey);
                Fill(32, y + 9, 56, 4, white);
                Fill(32, y + 2, 40, 4, white);
            }
            for (int column = 0; column < 3; column++)
            {
                int x = 9 + column * 30;
                Fill(x, 40, 24, 24, white);
                Fill(x + 5, 45, 14, 14, column == 0 ? new Color(0.05f, 0.6f, 0.2f) : column == 1 ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.4f, 0.85f));
            }
            Fill(8, 8, width - 16, 20, white);
            Fill(14, 13, 24, 10, grey);
            Fill(46, 18, 40, 3, grey);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            Directory.CreateDirectory(TextureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D PerforationTexture()
        {
            string path = $"{TextureFolder}/T_SpeakerGrille.png";
            var existing = ExistingTexture(path);
            if (existing != null) return existing;

            const int size = 128;
            const int cells = 16;
            const float cell = size / (float)cells;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x % cell - cell / 2f + 0.5f;
                    float dy = y % cell - cell / 2f + 0.5f;
                    bool hole = dx * dx + dy * dy < 2.7f * 2.7f;
                    pixels[y * size + x] = hole ? new Color(0.1f, 0.1f, 0.11f) : new Color(0.86f, 0.87f, 0.86f);
                }
            }
            texture.SetPixels(pixels);
            Directory.CreateDirectory(TextureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Writes a generated texture (pixel colours from <paramref name="pixel"/>); an existing file is kept, except once per <see cref="RegenerateTextures"/>.</summary>
        private static Texture2D GeneratedTexture(string name, int width, int height, System.Func<int, int, Color> pixel, int anisoLevel = 1)
        {
            string path = $"{TextureFolder}/{name}.png";
            var existing = ExistingTexture(path);
            if (existing != null) return existing;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel(x, y);
                }
            }
            texture.SetPixels(pixels);
            Directory.CreateDirectory(TextureFolder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            if (anisoLevel > 1 && AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.anisoLevel = anisoLevel;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Coarse hexagonal grille of a rack door, one tile of <c>Props.RackHexTile</c> m wide and 1.732 times as high:
        /// dark steel with wide, mostly transparent holes (alpha blended, mipmapped and anisotropic, so it does not shimmer when seen
        /// from the side).
        /// </summary>
        private static Texture2D HexPerforationTexture() => GeneratedTexture("T_RackGrille", 64, 111, (x, y) =>
        {
            float u = (x + 0.5f) / 64f;
            float v = (y + 0.5f) / 111f * 1.732f;
            float best = float.MaxValue;
            foreach (var c in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.866f), new Vector2(0f, 1.732f), new Vector2(1f, 1.732f) })
            {
                best = Mathf.Min(best, (new Vector2(u, v) - c).magnitude);
            }
            float shade = 0.05f + 0.02f * Mathf.Clamp01(1f - best * 2f);
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.36f, 0.42f, best));
            return new Color(shade, shade, shade * 1.05f, Mathf.Lerp(0.14f, 1f, edge));
        }, 8);

        /// <summary>
        /// One tile of the cable tray's slotted sheet (0.09 m along x 0.04 m across): a stadium-shaped slot 50 x 13 mm, alpha 0 inside
        /// (<c>InfrastructureModels</c> maps it at that scale).
        /// </summary>
        private static Texture2D TraySlotTexture() => GeneratedTexture("T_TraySlots", 144, 64, (x, y) =>
        {
            float u = ((x + 0.5f) / 144f - 0.5f) * 0.09f;
            float v = ((y + 0.5f) / 64f - 0.5f) * 0.04f;
            float dx = Mathf.Max(Mathf.Abs(u) - 0.0185f, 0f);
            float distance = Mathf.Sqrt(dx * dx + v * v);
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.0058f, 0.0072f, distance));
            return new Color(0.66f, 0.68f, 0.7f, alpha);
        }, 4);

        /// <summary>
        /// The glow of a rack door, drawn on its outline: a thin soft line along the edge of the leaf and along the inner edges of the frame
        /// (hinge stile 45 mm, handle stile 65 mm, rails 100 mm of a 0.596 x 1.9 m door), nearly clear over the grille panel, so the grille keeps its structure.
        /// </summary>
        private static Texture2D RackGlowTexture() => GeneratedTexture("T_RackGlow", 128, 256, (x, y) =>
        {
            const float w = 0.596f, h = 1.9f;
            float px = (x + 0.5f) / 128f * w;
            float py = (y + 0.5f) / 256f * h;
            float outer = Mathf.Min(Mathf.Min(px, w - px), Mathf.Min(py, h - py));
            // Distance to the inner edge of the frame (the grille opening), negative inside the frame.
            float inner = Mathf.Max(Mathf.Max(0.045f - px, px - (w - 0.065f)), Mathf.Max(0.10f - py, py - (h - 0.10f)));
            float alpha = 0.14f * Mathf.Exp(-outer * 160f) + 0.16f * Mathf.Exp(-Mathf.Abs(inner) * 220f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        });

        /// <summary>
        /// One 600 mm tile of the hall ceiling: concrete with a hairline joint round it and a 7 x 7 field of round perforations, in a lightly mottled grey
        /// (the ceiling is one quad that repeats it).
        /// </summary>
        private static Texture2D CeilingTileTexture() => GeneratedTexture("T_CeilingTile600", 256, 256, (x, y) =>
        {
            float u = (x + 0.5f) / 256f;
            float v = (y + 0.5f) / 256f;
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            if (edge < 0.006f) return new Color(0.2f, 0.2f, 0.2f);
            // Perforations on a 7 x 7 grid inside the tile.
            float gu = (u - 0.1f) / 0.8f * 7f;
            float gv = (v - 0.1f) / 0.8f * 7f;
            bool inside = u > 0.1f && u < 0.9f && v > 0.1f && v < 0.9f;
            float du = gu - Mathf.Floor(gu) - 0.5f;
            float dv = gv - Mathf.Floor(gv) - 0.5f;
            if (inside && du * du + dv * dv < 0.17f * 0.17f) return new Color(0.07f, 0.07f, 0.075f);
            float noise = Mathf.PerlinNoise(u * 40f, v * 40f) * 0.06f;
            float shade = 0.56f + noise;
            return new Color(shade, shade, shade * 0.98f);
        });

        /// <summary>
        /// One 400 mm panel of the lift ceiling: matt brushed stainless in exactly the grey of the car's walls (a faint grain along the panel) with a dark joint
        /// round it and a thin bright bevel inside the joint. Seamless, since the joint hides the edge.
        /// </summary>
        private static Texture2D LiftCeilingTexture() => GeneratedTexture("T_LiftCeiling", 512, 512, (x, y) =>
        {
            const int size = 512;
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            int edgePixels = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
            if (edgePixels < 2) return new Color(0.07f, 0.07f, 0.08f);
            // Brushing runs along u. The grey is exactly the car walls' (StainlessSteel, 0.30 / 0.31 / 0.32) with only a faint grain, so the ceiling and
            // the walls read as the same steel; the joints are the only visible structure.
            float streak = (Mathf.PerlinNoise(u * 6f, v * 220f) - 0.5f) * 0.03f;
            float grain = (((x * 73856093 ^ y * 19349663) & 255) / 255f - 0.5f) * 0.012f;
            float shade = streak + grain;
            if (edgePixels < 5) shade += 0.025f * (1f - (edgePixels - 2) / 3f);
            return new Color(0.30f + shade, 0.31f + shade, 0.32f + shade);
        });

        /// <summary>
        /// A 0.1 m square of the perforated floor tile: 4 x 4 round holes at a 25 mm pitch, every second row shifted by half a pitch. Powder-coated steel in the
        /// grey of the hall floor with fine paint grain and a few scratches, a chamfered lighter rim round every hole, and in the hole a dark gradient (the
        /// plenum), darker towards the middle. Seamless.
        /// </summary>
        private static Texture2D FloorTileTexture() => GeneratedTexture("T_FloorTilePerforated", 256, 256, (x, y) =>
        {
            const float cells = 4f;
            float u = (x + 0.5f) / 256f * cells;
            float v = (y + 0.5f) / 256f * cells;
            int row = Mathf.FloorToInt(v);
            float shift = (row % 2 == 0) ? 0f : 0.5f;
            float fu = u + shift;
            float cellU = fu - Mathf.Floor(fu) - 0.5f;
            float cellV = v - Mathf.Floor(v) - 0.5f;
            float distance = Mathf.Sqrt(cellU * cellU + cellV * cellV);
            const float radius = 0.27f;
            float grain = (Mathf.PerlinNoise(x * 0.35f, y * 0.35f) - 0.5f) * 0.05f + (((x * 73856093) ^ (y * 19349663)) & 255) / 255f * 0.025f;
            float scratch = Mathf.PerlinNoise(x * 0.02f, y * 0.9f) > 0.78f ? 0.03f : 0f;
            var paint = new Color(0.37f, 0.39f, 0.41f);
            if (distance < radius)
            {
                float depth = Mathf.Clamp01(distance / radius);
                float shade = Mathf.Lerp(0.012f, 0.07f, depth * depth);
                return new Color(shade, shade, shade * 1.1f);
            }
            float rim = distance - radius;
            float bevel = rim < 0.045f ? Mathf.Lerp(0.16f, 0f, rim / 0.045f) : 0f;
            // A soft darker halo of dirt round the rim.
            float halo = rim < 0.12f ? -0.05f * (1f - rim / 0.12f) : 0f;
            float lift = grain + scratch + bevel + halo;
            return new Color(paint.r + lift, paint.g + lift, paint.b + lift);
        }, 4);

        /// <summary>Honeycomb vent of a drive carrier, one tile of <c>Props.HoneycombTile</c> m wide: dark hexagonal holes in grey plastic.</summary>
        private static Texture2D HoneycombTexture() => GeneratedTexture("T_ServerHoneycomb", 64, 111, (x, y) =>
        {
            float u = (x + 0.5f) / 64f;
            float v = (y + 0.5f) / 111f * 1.732f;
            float best = float.MaxValue;
            foreach (var c in new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.866f), new Vector2(0f, 1.732f), new Vector2(1f, 1.732f) })
            {
                best = Mathf.Min(best, (new Vector2(u, v) - c).magnitude);
            }
            return best < 0.4f ? new Color(0.004f, 0.004f, 0.005f) : new Color(0.1f, 0.1f, 0.105f);
        });

        /// <summary>
        /// One cell of the drive-bay covers of the 3 U chassis (<c>Props.ChassisVentTile</c> square): a tall slot and a round hole under it in
        /// black sheet steel, with a faint lighter rim round each hole where the punched edge catches the light.
        /// </summary>
        private static Texture2D ChassisVentTexture() => GeneratedTexture("T_ChassisVent", 64, 64, (x, y) =>
        {
            float u = (x + 0.5f) / 64f;
            float v = (y + 0.5f) / 64f;
            // Slot (rounded ends) in the upper part, a round hole in the lower.
            float slotDistance = SlotDistance(u, v, 0.5f, 0.50f, 0.07f, 0.17f);
            float dotDistance = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.13f) * (v - 0.13f)) - 0.07f;
            float d = Mathf.Min(slotDistance, dotDistance);
            if (d < 0f) return new Color(0.004f, 0.004f, 0.005f);
            if (d < 0.035f) return new Color(0.11f, 0.11f, 0.12f);
            float grain = Mathf.PerlinNoise(u * 18f, v * 18f) * 0.012f;
            return new Color(0.045f + grain, 0.046f + grain, 0.05f + grain);
        });

        /// <summary>Signed distance to a vertical slot (a rounded segment) centred on (cx, cy), half width r, half length of the straight part l.</summary>
        private static float SlotDistance(float u, float v, float cx, float cy, float r, float l)
        {
            float dy = Mathf.Max(Mathf.Abs(v - cy) - l, 0f);
            return Mathf.Sqrt((u - cx) * (u - cx) + dy * dy) - r;
        }

        /// <summary>
        /// The LCD of the rack UPS: a blue 96 x 40 pixel display with two lines of light text in a 5 x 7 pixel font ("ONLINE MODE", "OUT:230V 49.9HZ").
        /// </summary>
        private static Texture2D UpsDisplayTexture() => GeneratedTexture("T_UpsDisplay", 96, 40, (x, y) =>
        {
            var glyphs = LcdGlyphs;
            int row = 39 - y;
            float shade = 0.9f + 0.1f * (y / 39f);
            var background = new Color(0.10f * shade, 0.24f * shade, 0.92f * shade);
            // Two text lines of 7 pixel rows each, starting at rows 8 and 21.
            foreach (var (text, top) in new[] { ("ONLINE MODE", 8), ("OUT:230V 49.9HZ", 21) })
            {
                int textRow = row - top;
                int column = x - 3;
                if (textRow < 0 || textRow >= 7 || column < 0) continue;
                int index = column / 6;
                if (index >= text.Length) continue;
                int inside = column % 6;
                if (inside < 5 && glyphs[text[index]][textRow][inside] == '#') return new Color(0.86f, 0.92f, 1f);
            }
            return background;
        });

        private static readonly Dictionary<char, string[]> LcdGlyphs = new Dictionary<char, string[]>
        {
            { 'O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
            { 'L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" } },
            { 'I', new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
            { 'M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
            { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
            { 'U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
            { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" } },
            { ':', new[] { ".....", "..#..", "..#..", ".....", "..#..", "..#..", "....." } },
            { '.', new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." } },
            { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
            { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
            { '3', new[] { ".###.", "#...#", "....#", "..##.", "....#", "#...#", ".###." } },
            { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
            { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "#...#", ".###." } },
            { ' ', new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." } },
        };

        /// <summary>Resets every material to the look defined here, keeping the asset (and its GUID). Rebuild only.</summary>
        public static void ResetAll()
        {
            foreach (var pair in Looks)
            {
                var material = Get(pair.Key);
                ApplyLook(material, pair.Value);
                EditorUtility.SetDirty(material);
            }
            ApplyTextures();
        }

        private static void ApplyLook(Material material, Look look)
        {
            ApplyTransparency(material, look.Alpha < 1f);
            material.SetColor("_BaseColor", look.Color);
            material.SetFloat("_Metallic", look.Metallic);
            material.SetFloat("_Smoothness", look.Smoothness);
            if (look.Emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", look.Emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }

        /// <summary>Switches a URP Lit material between opaque and alpha-blended transparent.</summary>
        private static void ApplyTransparency(Material material, bool transparent)
        {
            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            material.renderQueue = transparent ? (int)UnityEngine.Rendering.RenderQueue.Transparent : -1;
            if (transparent) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }
}
