GET_PAL - a 13 color NES image converter
(c) Doug Fraker, 2021

.NET 4.5.2 (works with MONO on non-Windows systems)

Updates -
1.4 - renamed it to quiet false alarm on download
    - issue with loading images with transparency
      now blanks to black before loading.

update, you can now save as .dz4 compression file 
(a file system also by Doug Fraker).


Quick How To...
1.File / Load Image
2.Click "Auto Generate Palette"
3.Click "Convert"
4.Palette / Save NES (palette)
5.CHR NT / Save Final CHR (graphics)
6.CHR NT / Save NT and AT (nametable with attribute 
  table)


typical behavior --- 
-Rt Click = Get color
-Lt Click = Set color


PRISM_NES: a tool to convert a full color image to NES 
colors, 4 full BG palettes (13 total colors), strict to 
16x16 attribute table limits. And, to output a CHR set, 
nametable with attribute table, and full BG palette, to 
assist in NES game development.

Image should be 256 x 240. Check the resize checkbox to 
auto resize if the image is bigger than that (may have 
to reload image)


Load an Image or Paste from the Clipboard. .png, .jpg, 
.bmp, .gif


Auto Generate Palette, tries to guess the ideal palette. 
You could also click on the image (before it's converted) 
and then one of the palette boxes... 

or (before converted) click on the NES palette to
select a color from it, and then click one of
the palette boxes.

Auto Generate will choose the most common color as
the main BG color. This might not be what you want.
To force Auto Gen to use a specific color as the
main BG color, click Override and select a color 
and set the Override Color. Then click Auto Generate

Choose a dither style (Bayer 8x8 or Floyd Steinberg) 
and dither amount 0-12, with 10 being "normal". Although 
5-7 is probably the best. 

! I DON'T recommend using Floyd Steinberg. It is buggy, 
and it increases tile count signigicantly.

Optional Setting, adjusing the brightness. 
Usually, don't touch this unless there is a problem.

Click Convert. This may take a few seconds. It should 
tell you (at the bottom) how many tiles it will have. 
You want 256 or less.

Option - You can turn off some of the palettes by un-
checking their boxes (far right "on / off")

ONCE CONVERTED...

If you don't like the auto palette choice for a 
16x16 box, you can manually change it by clicking on 
the image.
Change the "Palette to Set" Selection Box to 
choose Pal #1-4, then Lt click on the image (to 
change the palette).
Rt clicking the image will GET a palette choice.
Lt clicking will SET a palette choice.

Alternatively, Change the L Click = Rotate, and then
just L click the area with the wrong palette and it
will cycle through the choices.


Once you have it the way you want,
-Save the Palette (in NES format) 
-Save the CHR file with "Save Final CHR"
You probably don't want the "raw" versions.
The "FINAL CHR" is CHR from every palettes combined,
keeping only the tiles used.

Probably, you want to have "pad CHR to $1000" checkbox 
checked. Other apps that load tiles will expect size 
to be a multiple of $1000 (1000 hex). It just zero 
fills until it reaches that size.

Now, save the nametable data. "CHR_NT / save NT and AT", 
to save the NES format tilemap. Alternatively, you could 
save just the nametable part, or just the attribute 
table part. This data is uncompressed.

Saving as DZ4 will compress the file. Nametables should 
compress well. Use DZ4.asm to decompress.


Keep an eye on the # of tiles at the bottom of the app. 
You want 256 or less. If you have too many tiles, you 
could (using some photo editor app)
-shrink or crop the original picture
-simplify the background of the image (flat tone)
-turn off dithering (zero)
-blur or mosaic the original picture (despeckle)


Other things-
After an image is converted, if you click "revert" it 
will return to the original image.

Clicking the original pic will select a color from it.

After an image is converted, you can save it as an 
image. It will save the current view.

If there are only 4 colors (or less) detected, it will
only generate 1 palette.

Auto generate may generate up to 6 palettes. The extra
2 palettes are just some suggestions / guesses.
They don't do anything, except to show you some more 
alternatives.


NOTE - override main color only applies to the
Auto Generate Button. You can change the main
color at any time, and when you go to save the
palette, whatever is in the "main color" box
will be what is saved.




