# hdre

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
