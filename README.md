# HDRE: HDR file format for 3D environments

## Description

A **HDRE** is a binary file containing the information of a cubemap environment and some of the computed mipmapped versions needed in Photorealistic Rendering (PBR) to simulate different materials (Prefiltering steps).  It also contains other information as the values for the spherical harmonics (since v1.5) or the maximum luminance.

Since v2.0 there is an available version for C++ applications. 

## Structure

### Header

Contains all the HDRE properties. Default size: ```256 bytes``` [v2.0].

| Property                  |  Size      |
| ------------------------- | ---------: |
| Header signature ("HDRE") |  `4 bytes` |
| Version                   |  `4 bytes` |
| Width                     |  `2 bytes` |
| Height                    |  `2 bytes` |
| Max file size             |  `4 bytes` |
| Number of channels        |  `2 bytes` |
| Bits per channel          |  `2 bytes` |
| Header size               |  `2 bytes` |
| Encoding (LE)             |  `2 bytes` |
| Maximum luminance         |  `4 bytes` |
| Data type (UInt, Float..) |  `2 bytes` |
| SH (9 coef) [v2.0]        |            |

### Pixel data

The maximum size of a texture stored in HDRE is 256x256 pixels **per face**. Each prefiltered level is stored using half the size of the previous with a minimum value of 8x8. In the case of a 256 sized HDRE, the *mipmap* levels would be:

* Mip0: 256x256
* Mip1: 128x128
* Mip2: 64x64
* Mip3: 32x32
* Mip4: 16x16
* Mip5: 8x8

Note: Faces are stored individually, removing empty spaces when using cubemaps. 

![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/stadium-cubemap.jpg)
![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/qud.jpg)
