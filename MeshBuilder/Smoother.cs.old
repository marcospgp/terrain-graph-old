using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.Terrain {
    public static class Smoother {
        public static (
            Dictionary<Vector3Int, Vector3>,
            Dictionary<Vector3Int, Vector3>
        ) SmoothMesh(
            Dictionary<Vector3Int, Vector3> chunkVertices,
            Dictionary<Vector3Int, Vector3> borderVertices,
            float factor
        ) {
            // Tested averaging vectors with both all 26 neighbors and only the
            // 6 neighbors that are aligned with each axis. Latter works much
            // better.
            Vector3Int[] directions = {
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                // new Vector3Int(0, 1, 1),
                // new Vector3Int(0, 1, -1),
                // new Vector3Int(0, -1, 1),
                // new Vector3Int(0, -1, -1),
                // new Vector3Int(1, 0, 1),
                // new Vector3Int(1, 0, -1),
                // new Vector3Int(-1, 0, 1),
                // new Vector3Int(-1, 0, -1),
                // new Vector3Int(1, 1, 0),
                // new Vector3Int(1, -1, 0),
                // new Vector3Int(-1, 1, 0),
                // new Vector3Int(-1, -1, 0),
                // new Vector3Int(1, 1, 1),
                // new Vector3Int(1, 1, -1),
                // new Vector3Int(1, -1, 1),
                // new Vector3Int(1, -1, -1),
                // new Vector3Int(-1, 1, 1),
                // new Vector3Int(-1, 1, -1),
                // new Vector3Int(-1, -1, 1),
                // new Vector3Int(-1, -1, -1)
            };

            Dictionary<Vector3Int, Vector3> SmoothVertices(
                Dictionary<Vector3Int, Vector3> vertices,
                Dictionary<Vector3Int, Vector3> extraNeighbors
            ) {
                // Ensure that newly smoothed vertices do not affect neighbors
                // by updating them all at once
                var newVertices =
                    new Dictionary<Vector3Int, Vector3>(vertices);

                // Loop through previousVertices because vertices is being
                // modified in the loop, and looping through its keys would
                // throw an error
                foreach (Vector3Int pos in vertices.Keys) {
                    var neighbors = new List<Vector3>();

                    foreach (Vector3Int direction in directions) {
                        Vector3Int neighbor = pos + direction;

                        if (
                            vertices
                                .TryGetValue(neighbor, out Vector3 position) ||
                            extraNeighbors
                                .TryGetValue(neighbor, out position)
                        ) {
                            neighbors.Add(position);
                        }
                    }

                    Vector3 avg = Vector3.zero;

                    foreach (Vector3 neighbor in neighbors) {
                        avg += neighbor;
                    }

                    avg /= neighbors.Count;

                    newVertices[pos] = Vector3.Lerp(
                        vertices[pos],
                        avg,
                        factor
                    );
                }

                return newVertices;
            }

            Dictionary<Vector3Int, Vector3> newChunkVertices =
                SmoothVertices(chunkVertices, extraNeighbors: borderVertices);
            Dictionary<Vector3Int, Vector3> newBorderVertices =
                SmoothVertices(borderVertices, extraNeighbors: chunkVertices);

            return (newChunkVertices, newBorderVertices);
        }
    }
}
