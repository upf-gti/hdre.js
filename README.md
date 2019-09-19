# HDRE: HDR file format for 3D environments

## Description

A **HDRE** is a binary file containing the information of a cubemap environment and some of the computed mipmapped versions needed in Photorealistic Rendering (PBR) to simulate different materials (Prefiltering steps).  It also contains other information as the values for the spherical harmonics (since v1.5) or the maximum luminance.

Since v2.0 there is an available version for C++ applications. 

## Structure

### Header

Contains all the HDRE properties. Default size: ```256 bytes``` [v2.0].

 * Header signature ("HDRE")                ```4 bytes```
 * Version                                  ```4 bytes```
 * Width                                    ```2 bytes```
 * Height                                   ```2 bytes```
 * Max file size                            ```4 bytes```
 * Number of channels                       ```1 byte```
 * Bits per channel                         ```1 byte```
 * Header size                              ```1 byte```
 * Encoding (LE)                            ```1 byte```
 * ...
 * Maximum luminance                        ```4 bytes```
 * Data type (UInt, Half Float, Float)      ```2 bytes```
 * Values for Spherical Harmonics (9 coef)  [v2.0]

### Pixel data

The maximum size of a texture stored in HDRE is 256x256 pixels **per face**. Each prefiltered level is stored using half the size of the previous with a minimum value of 8x8. In the case of a 256 sized HDRE, the *mipmap* levels would be:

* Mip0: 256x256
* Mip1: 128x128
* Mip2: 64x64
* Mip3: 32x32
* Mip4: 16x16
* Mip5: 8x8

Note: Faces are stored individually, removing empty spaces when using cubemaps. 

## Use

### Write

To write an HDRE file, call the following HDRE method:

```
HDRE.write( data, width, height, options )
```

where:

* ```data``` is the pixel data of each face per mipmap level:

  ```
  var data = [
    // for each mipmap 
    { width: w, height: h, pixelData: [ face1_array, face2_array, ..., face6_array ] },
    ...
    { ... }
  ]
  ```

* ```width``` is the width of the original environment
* ```height``` is the height of the original environment
* ```options```

  ```
  var options = {
    array: Uint8Array, // Uint8Array, Uint16Array, Float32Array
    format: "rgbe" // null in other case,
    sh: sh_values // coefficients for spherical harmonics in case of having them
  }
  ```
  
### Parse (Read)

To parse a HDRE, call the following method and it will return the data divided in **pixel data** and **header**.

```
HDRE.parse( buffer, options )
```

where:

* ```buffer``` is the arraybuffer returned by the request
* ```options```

  ```
  var options = {
    onprogress: function(e){  } // callback method for following the parse progress
  }
  ```

![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/Untitled-2.jpg)
![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/qud.jpg)
