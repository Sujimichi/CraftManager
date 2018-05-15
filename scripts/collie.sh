#Collie - rounds all the files up into release

rm -rf bin/Release/CraftManager


mkdir bin/Release/CraftManager -p
mkdir bin/Release/CraftManager/Plugins -p
mkdir bin/Release/CraftManager/Assets -p

cp bin/Release/*.dll bin/Release/CraftManager/Plugins/
cp CraftManager.version bin/Release/CraftManager/Plugins/

cp -a assets/*.* bin/Release/CraftManager/
mv bin/Release/CraftManager/*.jpg bin/Release/CraftManager/Assets/
mv bin/Release/CraftManager/*.png bin/Release/CraftManager/Assets/

cp LICENCE.txt bin/Release/CraftManager/LICENCE.txt


CMVER=$(ruby -e "i=%x(cat CraftManager.cs | grep version); i=i.split(';')[0].split('=')[1].sub(';','').gsub('\"','').strip; puts i;")
KSPVER=$(ruby -e "i=%x(cat CraftManager.cs | grep 'Built Against KSP'); i=i.split(' ').last; puts i")

echo "version $CMVER" > bin/Release/CraftManager/version

rm bin/Release/*.dll
rm bin/Release/*.dll.mdb

cd bin/Release
rm -rf $CMVER/

mkdir $CMVER
#rm CraftManager_$CMVER.zip
zip -r $CMVER/CraftManager.zip CraftManager/


rm -rf /home/sujimichi/KSP/dev_KSP-$KSPVER/GameData/CraftManager/
cp -R CraftManager/ /home/sujimichi/KSP/dev_KSP-$KSPVER/GameData/CraftManager/

#rm -rf /home/sujimichi/Share/KSP/CraftManager/
#cp -R CraftManager/ /home/sujimichi/Share/KSP/CraftManager/
