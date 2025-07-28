using Assimp;
using Assimp.Export;
using Revise.ZMD;
using Revise.ZMO;
using Revise.ZMS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using NLog;

namespace Rose2OgreExporter
{
    public class GltfExporter
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static void Export(BoneFile skeleton, List<MotionFile> motions, List<ModelFile> meshes, string up, string outputPath)
        {
            var scene = new Scene();
            scene.RootNode = new Node("RootNode");

            var material = new Material { Name = "DefaultMaterial" };
            scene.Materials.Add(material);

            var meshNodes = new List<Node>();

            for (int i = 0; i < meshes.Count; i++)
            {
                var zms = meshes[i];
                var mesh = new Assimp.Mesh($"Mesh_{i}", PrimitiveType.Triangle);
                mesh.MaterialIndex = 0;

                for (int j = 0; j < zms.Vertices.Count; j++)
                {
                    var vertex = zms.Vertices[j];
                    mesh.Vertices.Add(new Vector3D(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                    if (zms.NormalsEnabled)
                    {
                        mesh.Normals.Add(new Vector3D(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z));
                    }
                    if (zms.TextureCoordinates1Enabled)
                    {
                        mesh.TextureCoordinateChannels[0].Add(new Vector3D(vertex.TextureCoordinates[0].X, vertex.TextureCoordinates[0].Y, 0));
                    }
                }

                foreach (var t in zms.Indices)
                {
                    mesh.Faces.Add(new Face(new int[] { t.A, t.B, t.C }));
                }

                if (zms.BonesEnabled)
                {
                    for (int j = 0; j < zms.BoneTable.Count; j++)
                    {
                        var boneIndex = zms.BoneTable[j];
                        var bone = skeleton.Bones[boneIndex];
                        var assimpBone = new Assimp.Bone { Name = bone.Name };

                        for (int k = 0; k < zms.Vertices.Count; k++)
                        {
                            var vertex = zms.Vertices[k];
                            for (int l = 0; l < 4; l++)
                            {
                                if (vertex.BoneIndices[l] == j)
                                {
                                    assimpBone.VertexWeights.Add(new VertexWeight(k, vertex.BoneWeights[l]));
                                }
                            }
                        }
                        mesh.Bones.Add(assimpBone);
                    }
                }
                scene.Meshes.Add(mesh);
                meshNodes.Add(new Node($"MeshNode_{i}", scene.RootNode) { MeshIndices = { i } });
            }

            scene.RootNode.Children.AddRange(meshNodes);

            foreach (var bone in skeleton.Bones)
            {
                var node = new Node(bone.Name);
                if (bone.Parent != -1)
                {
                    var parentNode = scene.RootNode.FindNode(skeleton.Bones[bone.Parent].Name);
                    if (parentNode != null)
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        scene.RootNode.Children.Add(node);
                    }
                }
                else
                {
                    scene.RootNode.Children.Add(node);
                }

                var translation = new Vector3D(bone.Translation.X, bone.Translation.Y, bone.Translation.Z);
                var rotation = new Assimp.Quaternion(bone.Rotation.W, bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z);
                node.Transform = Matrix4x4.CreateFromQuaternion(new System.Numerics.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W)) * Matrix4x4.CreateTranslation(new System.Numerics.Vector3(translation.X, translation.Y, translation.Z));
            }

            foreach (var motion in motions)
            {
                var animation = new Assimp.Animation
                {
                    Name = Path.GetFileNameWithoutExtension(motion.FilePath),
                    DurationInTicks = motion.FrameCount,
                    TicksPerSecond = motion.FramesPerSecond
                };

                foreach (var channel in motion.Channels)
                {
                    var bone = skeleton.Bones[channel.Index];
                    var nodeAnim = new NodeAnimationChannel { NodeName = bone.Name };

                    if (channel is PositionChannel positionChannel)
                    {
                        for (int i = 0; i < positionChannel.Frames.Count; i++)
                        {
                            var frame = positionChannel.Frames[i];
                            nodeAnim.PositionKeys.Add(new VectorKey(i, new Vector3D(frame.X, frame.Y, frame.Z)));
                        }
                    }
                    else if (channel is RotationChannel rotationChannel)
                    {
                        for (int i = 0; i < rotationChannel.Frames.Count; i++)
                        {
                            var frame = rotationChannel.Frames[i];
                            nodeAnim.RotationKeys.Add(new QuaternionKey(i, new Assimp.Quaternion(frame.W, frame.X, frame.Y, frame.Z)));
                        }
                    }
                    animation.NodeAnimationChannels.Add(nodeAnim);
                }
                scene.Animations.Add(animation);
            }

            var upVector = up?.ToUpper() switch
            {
                "X" => Vector3.UnitX,
                "Y" => Vector3.UnitY,
                "Z" => Vector3.UnitZ,
                _ => Vector3.UnitY,
            };

            var lookAt = Matrix4x4.CreateLookAt(Vector3.Zero, upVector, Vector3.UnitZ);
            var transform = new Matrix4x4(
                lookAt.M11, lookAt.M12, lookAt.M13, lookAt.M14,
                lookAt.M21, lookAt.M22, lookAt.M23, lookAt.M24,
                lookAt.M31, lookAt.M32, lookAt.M33, lookAt.M34,
                lookAt.M41, lookAt.M42, lookAt.M43, lookAt.M44
            );
            scene.RootNode.Transform = new Assimp.Matrix4x4(transform.M11, transform.M12, transform.M13, transform.M14,
                transform.M21, transform.M22, transform.M23, transform.M24,
                transform.M31, transform.M32, transform.M33, transform.M34,
                transform.M41, transform.M42, transform.M43, transform.M44);

            var exportFormat = "gltf2";
            var exporter = new AssimpExporter();
            exporter.Export(scene, exportFormat, outputPath);
            Logger.Info($"Exported scene to {outputPath}");
        }
    }
}
