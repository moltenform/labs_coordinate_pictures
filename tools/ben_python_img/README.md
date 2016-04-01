
ben_python_img
tools for resizing images and converting between formats

dependencies:
ben_python_common
   download github.com/downpoured/labs_coordinate_music/tree/master/ben_python_common
   place in directory ./tools/ben_python_img/ben_python_common
Pillow (a fork of PIL)
   pip install pillow
mozjpeg encoder
   unzip ./tools/mozjpeg_3.1_x86.zip
   (or download from github.com/mozilla/mozjpeg and build)
   either specify location from coordinate_pictures UI, (Options->set mozjpeg location)
   or edit ../options.ini to set FilepathMozJpeg=c:\path\to\cjpeg.exe
Optional: webp encoder
   download from https://developers.google.com/speed/webp/download
   either specify location from coordinate_pictures UI, (Options->set cwebp location)
   or edit ../options.ini to set FilepathWebp=c:\path\to\cwebp.exe
Optional: exiftool
   download from http://www.sno.phy.queensu.ca/~phil/exiftool/
   either specify location from coordinate_pictures UI, (Options->set exiftool location)
   or edit ../options.ini to set FilepathExifTool=c:\path\to\exiftool.exe
   