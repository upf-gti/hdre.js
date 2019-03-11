# HDRE file format

## Description

Binary file containing the information of a cubemap environment and the computed mipmapped versions needed in Photo Realistic Rendering (PBR) to simulate different materials.   

## Structure

### Header

HDRE information. Default size: ```128 bytes```.

 * Header signature ("HDRE")                ```4 bytes```
 * Format Version                           ```4 bytes```
 * Width                                    ```2 bytes```
 * Height                                   ```2 bytes```
 * Max file size                            ```2 bytes```
 * Number of channels                       ```1 byte```
 * Bits per channel                         ```1 byte```
 * Header size                              ```1 byte```
 * ...
 * Maximum luminance                        ```4 bytes```
 * Data type (8,16,32 bits)                 ```2 bytes```
 * Flags                                    ```1 byte```

### Pixel data

The maximum size of a texture stored in HDRE is 512x512 pixels. Each mipmap level is stored using half the size of the previous level. In case of a 512x512 float environment (32.76MBs):

* 0: 512x512
* 1: 256x256
* 2: 128x128
* 3: 64x64
* 4: 32x32
* 5: 16x16

Note: Faces stored individually removing empty spaces present in cubemap mapping. 

## Use

### Write

To write an HDRE file, call the following HDRE method:

```
HDRE.write( data, width, height, options )
```

where:

* data is the pixel data of each face per mipmap level:

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
* ```options``` (store as uint8array, uint16array or float32array)

![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/Untitled-2.jpg)
![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/qud.jpg)
