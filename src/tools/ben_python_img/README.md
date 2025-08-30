
### Introduction

Tools for resizing images and converting between formats.

Aims for high quality; uses recent builds of mozjpeg rather than PIL's built-in codecs.

### Usage

To resize images and keep exif metadata, see the (online documenation)[https://moltenform.com/page/labs-coordinate-pictures/doc/keeping-metadata.html]

Basically, you will rename files in the form "mypicture\_\_MARKAS\_\_50%.jpg", edit `img_resize_keep_exif.py` to point to the directory, and then run `img_resize_keep_exif.py`

To use ben\_python\_img from another Python script you can write something like this,

    import img_convert_resize
    img_convert_resize.convertOrResizeImage(
        'input.jpg', 'output.jpg', '50%', jpgQuality=95)
    img_convert_resize.convertOrResizeImage(
        'input.png', 'output.webp')

Additional features

* if jpgHighQualityChromaSampling is set to true, jpg files will be larger but will contain better sharpness, especially for red details.
* by default, img_resize_keep_exif will store filename in the Copyright exif tag.
* by default, img_resize_keep_exif will copy the most useful exif data over to the resized image.
* img_utils.readThumbnails to export jpg thumbnails to other jpgs, or remove thumbnail data.
* img_utils.removeResolutionTags to remove jpg resolution tags.
* img_utils.removeAllExifTags to remove all exif tags.
* One sometimes wants the resized jpg to have both dimensions be multiples of 8, as this allows more lossless transformation.
    * Instead of providing a percentage like 50%, provide a dimension like 288h.
    * This means that the smaller dimension (typically height) will be resized to 288 pixels (and aspect ratio preserved).
    * We'll show a warning if dimensions could not be made multiples of 8.

### Dependencies

* Pip libraries
    * pip install pillow
    * pip install shinerainsevenlib
* mozjpeg encoder
    * (GitHub releases come with this included)
    * unzip ./tools/mozjpeg\_4.0.3\_x86.zip
    * download from the internet, or apt get install
    * place cjpeg in the system path, or in ../mozjpeg/cjpeg
* webp encoder
    * (GitHub releases come with this included)
    * download from the internet, or apt get install
    * place cwebp in the system path, or in ../webp/cwebp
* exiftool
    * (GitHub releases come with this included)
    * download from the internet, or apt get install
    * place exiftool in the system path, or in ../exiftool/exiftool

