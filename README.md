# Terrain Graph

Graph Tools Foundation is a bit of a mess so had to include it here in part to be able to fix some things but also to do some heavy house cleaning in order to be able to compile it into a game build without including any of the editor-only code.

A lot of quirkiness comes from that architectural requirement, in particular the way the node previews are updated when playing around with the graph. It goes a bit like:

* Graph window asks node UI to update
* Node UI asks node logic to update
* Node UI receives updated values and uses them to repaint preview

With the added complexity that previews are cached for efficiency and that each node's preview depends on the cached values of the node before it (which may need refreshing) so there is more than one go-around.

## Possible improvements

### Slope blocks for smoother world

For a smoother world, consider generating slope blocks and not just cube
blocks. This should be simple based on surrounding voxels being air or ground.
Currently, slopes are noticeable after smoothing. This could also save on
smoothing iterations.

### Merge close vertices

As an alternative to the above, merge vertices whose distance is within a given threshold.

3 months ago I wrote that ^ now I actually get to implement it :O
