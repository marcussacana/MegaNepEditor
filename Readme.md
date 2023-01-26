# MegaNepEditor - v1.0.4

Can Unpack and Repack "MegaTagmension Blanc + Neptune VS Zombies" .cat files

This tool isn't compatible with all .cat files, tested only with the font ".\data\bootstrap\main_font.cat" and scripts files ".\data\script\scenario\*.cat"


### Usage:
- Drag and drop a .cat/.dpk file to extract
- Drag and drop a extracted cat directory to repack (Will be create with .new extension)

(To repack you can't move the directory, rename or delete the original .cat package)

### Notes:
Files with named like "AnyHexNumber.bin" means the package don't have names of the files, you need manually discovery the .bin file type or look in the files inside "Fnames" directory

The font `main_font.cat` when extracted contains 4 files,
- 00000000.dpk is the Font texture
- 00000001.bin is the half width characters width
- 00000002.bin font sjis character table
- 00000003.dpk buttons texture

I haven't made any tool to edit it automatically but is very easy to edit by hands.
- Update the texture at 00000000.dpk
- Find your modified character position (don't count the break lines) in 00000002.bin
- In the 00000001.bin, the position of the character that you found in the step 2 with plus 5, is the byte address of character width.  
So, for example, in 00000002.bin the character `a` position is 65, then 65+5=70, the byte at position 70 in 00000001.bin is the character width.  
And now, you can change the font texture and characters width :) 

### Screenshot:
![image](https://user-images.githubusercontent.com/10576957/214771240-e8bb80b2-6974-45e0-b73a-dc289b26ce9b.png)


