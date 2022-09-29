using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class VoxelBuilder {
        // Maps a direction to face vertices (clockwise, starting at bottom left corner).
        // Origin is XZ corner of voxel, so that vertices have integer coordinates.
        private static readonly Dictionary<Vector3Int, Vector3Int[]> faceVerticesMap =
            new Dictionary<Vector3Int, Vector3Int[]>() {
                {
                    new Vector3Int(0, 0, 1),
                    new Vector3Int[] {
                        new Vector3Int(1, 0, 1),
                        new Vector3Int(1, 1, 1),
                        new Vector3Int(0, 1, 1),
                        new Vector3Int(0, 0, 1)
                    }
                },
                {
                    new Vector3Int(0, 0, -1),
                    new Vector3Int[] {
                        new Vector3Int(0, 0, 0),
                        new Vector3Int(0, 1, 0),
                        new Vector3Int(1, 1, 0),
                        new Vector3Int(1, 0, 0)
                    }
                },
                {
                    new Vector3Int(0, 1, 0),
                    new Vector3Int[] {
                        new Vector3Int(0, 1, 0),
                        new Vector3Int(0, 1, 1),
                        new Vector3Int(1, 1, 1),
                        new Vector3Int(1, 1, 0)
                    }
                },
                {
                    new Vector3Int(0, -1, 0),
                    new Vector3Int[] {
                        new Vector3Int(0, 0, 1),
                        new Vector3Int(0, 0, 0),
                        new Vector3Int(1, 0, 0),
                        new Vector3Int(1, 0, 1)
                    }
                },
                {
                    new Vector3Int(1, 0, 0),
                    new Vector3Int[] {
                        new Vector3Int(1, 0, 0),
                        new Vector3Int(1, 1, 0),
                        new Vector3Int(1, 1, 1),
                        new Vector3Int(1, 0, 1)
                    }
                },
                {
                    new Vector3Int(-1, 0, 0),
                    new Vector3Int[] {
                        new Vector3Int(0, 0, 1),
                        new Vector3Int(0, 1, 1),
                        new Vector3Int(0, 1, 0),
                        new Vector3Int(0, 0, 0)
                    }
                }
            };

        public static void BuildVoxel(
            Vector3Int pos,
            bool[,,] densityMap,
            int borderWidth,
            Dictionary<Vector3Int, Vector3> chunkVertices,
            Dictionary<Vector3Int, Vector3> borderVertices,
            List<Vector3Int[]> chunkTriangles,
            List<Vector3Int[]> borderTriangles,
            List<Vector3Int> edgeVertices
        ) {
            int width = densityMap.GetLength(0);
            int depth = densityMap.GetLength(2);

            // The border is used to calculate correct smoothing and normals,
            // it is discarded afterwards.
            bool isBorder =
                pos.x < borderWidth ||
                pos.z < borderWidth ||
                pos.x >= width - borderWidth ||
                pos.z >= depth - borderWidth;

            // Edge is the outermost part of a chunk, ignoring the border.
            // It is stored to make sure we don't mess with edge vertices when
            // building lower polygon meshes for LODs.
            bool isEdge =
                pos.x == borderWidth ||
                pos.z == borderWidth ||
                pos.x == width - borderWidth - 1 ||
                pos.z == depth - borderWidth - 1;

            void BuildFace(Vector3Int face) {
                // To make chunk origin be its lowest XZ coordinate corner,
                // we have to take border width into account.
                var realPos = new Vector3Int(pos.x - borderWidth, pos.y, pos.z - borderWidth);

                VoxelBuilder.BuildFace(
                    realPos,
                    face,
                    chunkVertices,
                    borderVertices,
                    chunkTriangles,
                    borderTriangles,
                    edgeVertices,
                    isBorder,
                    isEdge
                );
            }

            // Build faces

            var directions = new Vector3Int[] {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)
            };

            foreach (Vector3Int direction in directions) {
                Vector3Int neighborPos = pos + direction;

                bool isOutOfBounds =
                    neighborPos.x < 0 ||
                    neighborPos.z < 0 ||
                    neighborPos.x >= densityMap.GetLength(0) ||
                    neighborPos.z >= densityMap.GetLength(2);

                bool buildFace;

                if (isOutOfBounds) {
                    // Do not build side faces at border
                    continue;
                } else if (neighborPos.y == -1) {
                    buildFace = false;
                } else if (neighborPos.y == densityMap.GetLength(1)) {
                    buildFace = true;
                } else {
                    // Build face if neighbor voxel it faces is empty
                    buildFace = !densityMap[neighborPos.x, neighborPos.y, neighborPos.z];
                }

                if (buildFace) {
                    BuildFace(direction);
                }
            }
        }

        private static void BuildFace(
            Vector3Int pos,
            Vector3Int direction,
            Dictionary<Vector3Int, Vector3> chunkVertices,
            Dictionary<Vector3Int, Vector3> borderVertices,
            List<Vector3Int[]> chunkTriangles,
            List<Vector3Int[]> borderTriangles,
            List<Vector3Int> edgeVertices,
            bool isBorder,
            bool isEdge
        ) {
            var faceVertices = new Vector3Int[4];

            Array.Copy(VoxelBuilder.faceVerticesMap[direction], faceVertices, 4);

            for (int i = 0; i < faceVertices.Length; i++) {
                faceVertices[i] += pos;
            }

            foreach (Vector3Int vertex in faceVertices) {
                // Vertices are shared between faces.
                // We ensure non-border vertices are added to chunkVertices,
                // so that they are not discarded.
                if (isBorder) {
                    if (chunkVertices.ContainsKey(vertex)) {
                        continue;
                    }

                    if (!borderVertices.ContainsKey(vertex)) {
                        borderVertices.Add(vertex, (Vector3) vertex);
                    }
                } else {
                    // Ensure vertex hasn't previously been added as a border
                    // vertex
                    _ = borderVertices.Remove(vertex);

                    if (!chunkVertices.ContainsKey(vertex)) {
                        chunkVertices.Add(vertex, (Vector3) vertex);
                    }

                    // Vertex may have been created when building a block with
                    // lower x or z coordinate before, but we still want it to
                    // be marked as an edge vertex.
                    if (isEdge) {
                        edgeVertices.Add(vertex);
                    }
                }
            }

            Vector3Int[] newTriangles = {
                new Vector3Int(0, 1, 2),
                new Vector3Int(0, 2, 3)
            };

            foreach (Vector3Int triangle in newTriangles) {
                List<Vector3Int[]> triangleList =
                    isBorder ? borderTriangles : chunkTriangles;

                triangleList.Add(new Vector3Int[] {
                    faceVertices[triangle.x],
                    faceVertices[triangle.y],
                    faceVertices[triangle.z]
                });
            }
        }
    }
}
