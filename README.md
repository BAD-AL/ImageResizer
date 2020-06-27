# ImageResizer

A command line program that resizes images from the command line. 

Usage:
    ImageResizer -infile:<inFileName> -outfile:<outFileName> -w:<width> -h:<height>
  or 
    ImageResizer <inFileName> -w:<width> -h:<height>
  or 
    ImageResizer <inFileName> -w:<width> 

Example: 
    ImageResizer -infile:Luke.TGA -outfile:SmallLuke.TGA -w:128 -h:128
Example: (Resize an image, if one dimension is omitted the given dimension will be used for both)
    ImageResizer -infile:Luke.TGA -w:128 

More options:
-smallerOnly    Only resize if it results in a smaller size; don't resize if it would make the image larger.


If you have a lot of TGA files to resize, you might want to do something like:

REM List all .tga files in the current directory and all sub directories to file 'resize.bat'
> dir /b /s *.tga > resize.bat 

Now edit 'resize.bat' using 'find and replace' with a text editor to craft a script that will resize your images.
