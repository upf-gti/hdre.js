## CREATE HDRE FILE FROM HDR IMAGE


**Supported formats: spheremap/panoramic**

1. Get the image. Recommended link [`https://hdrihaven.com/hdris/`](https://hdrihaven.com/hdris/)
(Download the image using 4K as max res)

Even if your file is an `exr`, make sure you don't skip steps 2,3. `OpenEXR` files can be written using many compressions and we don't support all of them!

2. Download mitsuba here: [`https://www.mitsuba-renderer.org/download.html`](https://www.mitsuba-renderer.org/download.html) 
([Windows version](https://www.mitsuba-renderer.org/releases/current/windows/))

3. Drag the image to Mitsuba and press `Ctrl + E` to export the image as `.exr` (File → export)

4. Drag the file into this [3D editor](https://webglstudio.org/projects/hdr4eu/latest/) and select the face size of the skybox (more resolution, more MBs…..)

5. In menu bar, click: **Tools → Cubemap Tools → Export** and save it to disc.
