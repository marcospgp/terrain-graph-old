# Terrain Graph

---

Deprecation notice:

This project has been abandoned - relying on Graph Tools Foundation from Unity was too much work. I spent too many hours cleaning that code and making it able to be compiled into a build, but ended up not being happy with the limitations of using it as a graph editor.

It has too many bugs and no official release. I was expecting the code to be cleaner and support to be stronger over time.

I'm instead going to move my UI to the browser as much as possible because Unity UI is too quirky and less accessible :)

---

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
