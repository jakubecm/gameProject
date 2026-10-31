using System.IO;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Builds the player's body: a mannequin of cylinders, spheres and boxes in a bone hierarchy, driven by
    /// <see cref="PlayerBodyAnimator"/>. All sizes come from <see cref="PlayerBodyDimensions"/>. The meshes
    /// have no colliders, so the body never blocks the CharacterController or the interaction ray. The head
    /// and the chest only cast shadows: the camera sits in the head, right above the chest.
    /// </summary>
    internal static class PlayerBodyBuilder
    {
        public const string MaterialPath = "Assets/_Project/Art/Materials/M_PlayerBody.mat";

        /// <summary>The body hangs slightly behind the camera, so the chest never fills the view when looking down.</summary>
        private const float BackOffset = 0.06f;

        /// <summary>Adds a "Body" child to a player and connects it to the player's controller.</summary>
        public static GameObject AddBody(GameObject player)
        {
            var material = LoadOrCreateMaterial();

            var body = new GameObject("Body");
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = BodyPosition(player);

            var pelvis = Joint("Pelvis", body.transform, new Vector3(0f, PlayerBodyDimensions.StandingHipHeight, 0f));
            Part("Mesh_Pelvis", PrimitiveType.Cube, pelvis, new Vector3(0f, 0.02f, 0f), new Vector3(0.32f, 0.16f, 0.2f), material);

            var chest = Joint("Chest", pelvis, new Vector3(0f, PlayerBodyDimensions.PelvisToChest, 0f));
            // The camera is 14 cm above the chest, so a chest drawn to it would fill half the screen when looking down.
            // Like the head, it only casts shadows; the legs and the arms are what the player sees of the body.
            var torso = Part("Mesh_Torso", PrimitiveType.Cube, chest, new Vector3(0f, PlayerBodyDimensions.TorsoHeight / 2f, 0f),
                new Vector3(0.38f, PlayerBodyDimensions.TorsoHeight, 0.2f), material);
            torso.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            var head = Part("Mesh_Head", PrimitiveType.Sphere, chest, new Vector3(0f, PlayerBodyDimensions.TorsoHeight + 0.15f, 0f), Vector3.one * 0.22f, material);
            head.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            var upperArmLeft = Joint("UpperArm_L", chest, new Vector3(-PlayerBodyDimensions.ShoulderHalfWidth, PlayerBodyDimensions.ShoulderHeight, 0f));
            var upperArmRight = Joint("UpperArm_R", chest, new Vector3(PlayerBodyDimensions.ShoulderHalfWidth, PlayerBodyDimensions.ShoulderHeight, 0f));
            var forearmLeft = BuildArm(upperArmLeft, material);
            var forearmRight = BuildArm(upperArmRight, material);

            var thighLeft = Joint("Thigh_L", pelvis, new Vector3(-PlayerBodyDimensions.HipHalfWidth, 0f, 0f));
            var thighRight = Joint("Thigh_R", pelvis, new Vector3(PlayerBodyDimensions.HipHalfWidth, 0f, 0f));
            BuildLeg(thighLeft, material, out var shinLeft, out var footLeft);
            BuildLeg(thighRight, material, out var shinRight, out var footRight);
            PoseStanding(thighLeft, shinLeft, footLeft);
            PoseStanding(thighRight, shinRight, footRight);

            var animator = body.AddComponent<PlayerBodyAnimator>();
            var serialized = new SerializedObject(animator);
            serialized.FindProperty("controller").objectReferenceValue = player.GetComponent<FirstPersonController>();
            SetBone(serialized, "pelvis", pelvis);
            SetBone(serialized, "chest", chest);
            SetBone(serialized, "thighLeft", thighLeft);
            SetBone(serialized, "thighRight", thighRight);
            SetBone(serialized, "shinLeft", shinLeft);
            SetBone(serialized, "shinRight", shinRight);
            SetBone(serialized, "footLeft", footLeft);
            SetBone(serialized, "footRight", footRight);
            SetBone(serialized, "upperArmLeft", upperArmLeft);
            SetBone(serialized, "upperArmRight", upperArmRight);
            SetBone(serialized, "forearmLeft", forearmLeft);
            SetBone(serialized, "forearmRight", forearmRight);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return body;
        }

        /// <summary>
        /// The body sits a little behind the camera. Its height above the floor is set every frame by the
        /// animator, which puts the soles on the actual floor under the player.
        /// </summary>
        public static Vector3 BodyPosition(GameObject player) => new Vector3(0f, 0f, -BackOffset);

        private static Transform BuildArm(Transform upperArm, Material material)
        {
            Part("Mesh_UpperArm", PrimitiveType.Cylinder, upperArm, new Vector3(0f, -PlayerBodyDimensions.UpperArmLength / 2f, 0f),
                Limb(0.09f, PlayerBodyDimensions.UpperArmLength), material);

            var forearm = Joint("Forearm", upperArm, new Vector3(0f, -PlayerBodyDimensions.UpperArmLength, 0f));
            Part("Mesh_Elbow", PrimitiveType.Sphere, forearm, Vector3.zero, Vector3.one * 0.09f, material);
            Part("Mesh_Forearm", PrimitiveType.Cylinder, forearm, new Vector3(0f, -PlayerBodyDimensions.ForearmLength / 2f, 0f),
                Limb(0.08f, PlayerBodyDimensions.ForearmLength), material);
            Part("Mesh_Hand", PrimitiveType.Sphere, forearm, new Vector3(0f, -PlayerBodyDimensions.ForearmLength - 0.02f, 0f), Vector3.one * 0.09f, material);
            return forearm;
        }

        private static void BuildLeg(Transform thigh, Material material, out Transform shin, out Transform foot)
        {
            Part("Mesh_Thigh", PrimitiveType.Cylinder, thigh, new Vector3(0f, -PlayerBodyDimensions.ThighLength / 2f, 0f),
                Limb(0.15f, PlayerBodyDimensions.ThighLength), material);

            shin = Joint("Shin", thigh, new Vector3(0f, -PlayerBodyDimensions.ThighLength, 0f));
            Part("Mesh_Knee", PrimitiveType.Sphere, shin, Vector3.zero, Vector3.one * 0.13f, material);
            Part("Mesh_Shin", PrimitiveType.Cylinder, shin, new Vector3(0f, -PlayerBodyDimensions.ShinLength / 2f, 0f),
                Limb(0.11f, PlayerBodyDimensions.ShinLength), material);

            foot = Joint("Foot", shin, new Vector3(0f, -PlayerBodyDimensions.ShinLength, 0f));
            Part("Mesh_Foot", PrimitiveType.Cube, foot, new Vector3(0f, -PlayerBodyDimensions.AnkleHeight / 2f, 0.06f),
                new Vector3(PlayerBodyDimensions.FootWidth, PlayerBodyDimensions.AnkleHeight, PlayerBodyDimensions.FootLength), material);
        }

        /// <summary>
        /// Rests the leg in its standing pose, so the feet are on the floor when the game is not running
        /// (the animator takes over in Play mode).
        /// </summary>
        private static void PoseStanding(Transform thigh, Transform shin, Transform foot)
        {
            float down = PlayerBodyDimensions.StandingHipHeight - PlayerBodyDimensions.AnkleHeight;
            var pose = LegSolver.Solve(PlayerBodyDimensions.ThighLength, PlayerBodyDimensions.ShinLength, 0f, down);
            thigh.localRotation = Quaternion.Euler(-pose.ThighAngle, 0f, 0f);
            shin.localRotation = Quaternion.Euler(pose.KneeFlexion, 0f, 0f);
            foot.localRotation = Quaternion.Euler(pose.ShinAngle, 0f, 0f);
        }

        /// <summary>Scale for the cylinder primitive (2 m tall, 1 m wide) to get a limb of the given thickness and length.</summary>
        private static Vector3 Limb(float thickness, float length) => new Vector3(thickness, length / 2f, thickness);

        private static Transform Joint(string name, Transform parent, Vector3 localPosition)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = localPosition;
            return joint;
        }

        private static GameObject Part(string name, PrimitiveType shape, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            // A body that collides would push the CharacterController and block the interaction ray.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        private static void SetBone(SerializedObject serialized, string field, Transform bone)
        {
            serialized.FindProperty(field).objectReferenceValue = bone;
        }

        private static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;

            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.30f, 0.34f, 0.40f));
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
