REM @echo off
for %%f in (*.png) do "C:\Program Files\ImageMagick-7.1.1-Q16-HDRI\magick.exe" "%%f" -quality 95 -colorspace sRGB -define heic:speed=0 "%%~nf.avif" "%%~nf.avif"
echo Done!
pause
