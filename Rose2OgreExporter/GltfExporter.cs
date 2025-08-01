using Assimp;
using NLog;
using Revise.ZMD;
using Revise.ZMO;
using Revise.ZMO.Channels;
using Revise.ZMS;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vector3D = Assimp.Vector3D;

namespace Rose2OgreExporter
{
    public class GltfExporter
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static void Export(BoneFile? skeleton, List<MotionFile> motions, List<ModelFile> meshes, string up, string outputPath)
        {
            var scene = new Scene
            {
                RootNode = new Node("RootNode")
            };

            var material = new Material { Name = "DefaultMaterial" };
            scene.Materials.Add(material);

            var meshNodes = new List<Node>();

            for (int i = 0; i < meshes.Count; i++)
            {
                var zms = meshes[i];

                var mesh = new Mesh($"Mesh_{i}", PrimitiveType.Triangle)
                {
                    MaterialIndex = 0
                };

                if (zms.BonesEnabled && skeleton != null)
                {
                    foreach (var bone in skeleton.Bones) 
                    {
                        var assimp_bone = new Assimp.Bone { Name = bone.Name };
                        mesh.Bones.Add(assimp_bone);
                    }

                    foreach(var dummy_bone in skeleton.DummyBones)
                    {
                        var assimp_dummy_bone = new Assimp.Bone { Name = dummy_bone.Name };
                        mesh.Bones.Add(assimp_dummy_bone);
                    }
                }

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

                    if (zms.TextureCoordinates2Enabled)
                    {
                        mesh.TextureCoordinateChannels[1].Add(new Vector3D(vertex.TextureCoordinates[1].X, vertex.TextureCoordinates[1].Y, 0));
                    }

                    if (zms.TextureCoordinates3Enabled)
                    {
                        mesh.TextureCoordinateChannels[2].Add(new Vector3D(vertex.TextureCoordinates[2].X, vertex.TextureCoordinates[2].Y, 0));
                    }

                    if (zms.TextureCoordinates4Enabled)
                    {
                        mesh.TextureCoordinateChannels[2].Add(new Vector3D(vertex.TextureCoordinates[3].X, vertex.TextureCoordinates[3].Y, 0));
                    }

                    if (zms.TangentsEnabled)
                    {
                        mesh.Tangents.Add(new Vector3D(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z));
                    }

                    if (zms.BonesEnabled && skeleton != null)
                    {
                        for (int vertex_bone_idx = 0; vertex_bone_idx < 4; vertex_bone_idx++)
                        {
                            var bone_idx = zms.BoneTable[vertex.BoneIndices[vertex_bone_idx]];
                            var bone = skeleton.Bones[bone_idx];
                            var assimp_bone = mesh.Bones[bone_idx];
                            assimp_bone.VertexWeights.Add(new VertexWeight(j, vertex.BoneWeights[vertex_bone_idx]));
                        }                        
                    }
                }

                Logger.Info("Vertices added");

                foreach (var t in zms.Indices)
                    mesh.Faces.Add(new Face([t.X, t.Y, t.Z]));
                Logger.Info("Faces added");
/*
                if (zms.BonesEnabled && skeleton != null)
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
                    Logger.Info("Bones added");
                }
*/

                scene.Meshes.Add(mesh);
                meshNodes.Add(new Node($"MeshNode_{i}", scene.RootNode) { MeshIndices = { i } });
            }

            scene.RootNode.Children.AddRange([.. meshNodes]);

            if (skeleton != null)
            {
                var boneNodes = new Dictionary<int, Node>();

                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    var zmd_bone = skeleton.Bones[i];
                    var node = new Node(zmd_bone.Name);
                    boneNodes.Add(i, node);

                    var transform_mat = Matrix4x4.Identity;

                    if (i > 0)
                    {
                        var parent_bone = skeleton.Bones[zmd_bone.Parent];
                        transform_mat =
                            Matrix4x4.FromTranslation(new Vector3D(parent_bone.Translation.X, parent_bone.Translation.Y, parent_bone.Translation.Z))
                            * new Matrix4x4((new Quaternion(parent_bone.Rotation.W, parent_bone.Rotation.X, parent_bone.Rotation.Y, parent_bone.Rotation.Z)).GetMatrix());
                    }

                    var translation_vector = new Vector3D(zmd_bone.Translation.X, zmd_bone.Translation.Y, zmd_bone.Translation.Z);
                    var rotation_quat = new Quaternion(zmd_bone.Rotation.W, zmd_bone.Rotation.X, zmd_bone.Rotation.Y, zmd_bone.Rotation.Z);
                    var rotation_matrix = rotation_quat.GetMatrix();

                    var arotMat = new Matrix4x4(rotation_matrix);
                    var atraMat = Matrix4x4.FromTranslation(translation_vector);
                    //node.Transform = transform_mat * atraMat * arotMat;
                    node.Transform = atraMat * arotMat;

                }

                for (int i = 0; i < skeleton.Bones.Count; i++)
                {
                    var bone = skeleton.Bones[i];
                    if (bone.Parent > 0)
                    {
                        var parentBone = boneNodes[bone.Parent];
                        parentBone.Children.Add(boneNodes[i]);
                    }
                    else
                    {
                        scene.RootNode.Children.Add(boneNodes[i]);
                    }
                }

                if (motions.Count != 0)
                {
                    foreach (var motion in motions)
                    {
                        var animation = new Animation
                        {
                            Name = Path.GetFileNameWithoutExtension(motion.FilePath),
                            DurationInTicks = motion.FrameCount,
                            TicksPerSecond = motion.FramesPerSecond
                        };

                        foreach (var channel in motion.Channels)
                        {
                            var bone = skeleton.Bones[channel.Index];

                            var nodeAnim = animation.NodeAnimationChannels.FirstOrDefault(c => c.NodeName.Equals(bone.Name));
                            if (nodeAnim == null)
                            {
                                nodeAnim = new NodeAnimationChannel { NodeName = bone.Name };
                                animation.NodeAnimationChannels.Add(nodeAnim);
                            }

                            if (channel is PositionChannel positionChannel)
                            {
                                for (int i = 0; i < positionChannel.Frames.Length; i++)
                                {
                                    var frame = positionChannel.Frames[i];
                                    nodeAnim.PositionKeys.Add(new VectorKey(i, new Vector3D(frame.X, frame.Y, frame.Z)));
                                }
                            }
                            else if (channel is RotationChannel rotationChannel)
                            {
                                for (int i = 0; i < rotationChannel.Frames.Length; i++)
                                {
                                    var frame = rotationChannel.Frames[i];
                                    nodeAnim.RotationKeys.Add(new QuaternionKey(i, new Assimp.Quaternion(frame.W, frame.X, frame.Y, frame.Z)));
                                }
                            }
                            else if (channel is ScaleChannel scaleChannel)
                            {
                                for (int i = 0; i < scaleChannel.Frames.Length; i++)
                                {
                                    var frame = scaleChannel.Frames[i];
                                    nodeAnim.ScalingKeys.Add(new VectorKey(i, new Vector3D(frame)));
                                }
                            }
                        }
                        scene.Animations.Add(animation);
                    }
                }
            }
            /*
                        var upVector = up?.ToUpper() switch
                        {
                            "X" => Vector3.UnitX,
                            "Y" => Vector3.UnitY,
                            "Z" => Vector3.UnitZ,
                            _ => Vector3.UnitY,
                        };

                        var lookAt = System.Numerics.Matrix4x4.CreateLookAt(Vector3.Zero, upVector, Vector3.UnitZ);
                        var transform = new Assimp.Matrix4x4(
                            lookAt.M11, lookAt.M12, lookAt.M13, lookAt.M14,
                            lookAt.M21, lookAt.M22, lookAt.M23, lookAt.M24,
                            lookAt.M31, lookAt.M32, lookAt.M33, lookAt.M34,
                            lookAt.M41, lookAt.M42, lookAt.M43, lookAt.M44
                        );
                        scene.RootNode.Transform = transform;
            */
            var exportFormat = "gltf2";
            var context = new AssimpContext();

            var export_dir = Path.GetDirectoryName(outputPath);
            var file_name = Path.GetFileName(outputPath);

            var current_path = Directory.GetCurrentDirectory();

            Directory.SetCurrentDirectory(export_dir);

            context.ExportFile(scene, file_name, exportFormat, PostProcessSteps.ValidateDataStructure);
            Logger.Info($"Exported scene to {outputPath}");

            Directory.SetCurrentDirectory(current_path);


            //if (context.IsExportFormatSupported(".dae"))
            //    context.ExportFile(scene, Path.ChangeExtension(outputPath, ".dae"), "collada", PostProcessSteps.ValidateDataStructure);
            //else
            //    Logger.Warn("Format dae not supported");

        }
    }
}
