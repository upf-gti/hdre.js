# HDRE file format

## Description

Binary file containing the information of a cubemap environment and its **5 levels** of blurrines needed in Photo Realistic Rendering (PBR) to simulate a roughness material.   

## Structure

### Header

Size: ```164 bytes```

 * Header signature ("HDRE" in ASCII)       ```4 bytes```
 * Format Version                           ```4 bytes```
 * Width                                    ```2 bytes```
 * Height                                   ```2 bytes```
 * Max file size                            ```2 bytes```
 * Number of channels                       ```1 byte```
 * Bits per channel                         ```1 byte```
 * Flags                                    ```1 byte```

### Pixel data

The maximum size of a texture stored in HDRE is 512x512 pixels. Each level of blur is stored using half the size of the previous level. In case of a 512x512 environment (32.76MBs):

* 0: 256x256
* 1: 128x128
* 2: 64x64
* 3: 32x32
* 4: 16x16

#### Content

All pixels data is stored as Float32Arrays using the level order.

![HDRE pixel storage](https://webglstudio.org/users/arodriguez/screenshots/levels.PNG)
