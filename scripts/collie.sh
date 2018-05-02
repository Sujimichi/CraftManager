#Collie - rounds all the files up into release

rm -rf bin/Release/CraftManager
rm bin/Release/CraftManager.zip

mkdir bin/Release/CraftManager -p
mkdir bin/Release/CraftManager/Plugins -p
mkdir bin/Release/CraftManager/Assets -p

cp bin/Release/*.dll bin/Release/CraftManager/Plugins/
cp CraftManager.version bin/Release/CraftManager/Plugins/

cp -a assets/*.* bin/Release/CraftManager/
mv bin/Release/CraftManager/*.jpg bin/Release/CraftManager/Assets/
mv bin/Release/CraftManager/*.png bin/Release/CraftManager/Assets/

cp LICENCE.txt bin/Release/CraftManager/LICENCE.txt

#ruby -e "i=%x(cat Source/KerbalX.cs | grep version); i=i.split('=')[1].sub(';','').gsub('\"','').strip; s=\"echo 'version: #{i}' > bin/Release/KerbalX/version\"; system(s)"


rm bin/Release/*.dll
rm bin/Release/*.dll.mdb

cd bin/Release
zip -r CraftManager.zip CraftManager/


rm -rf /home/sujimichi/KSP/dev_KSP-1.4.3/GameData/CraftManager/
cp -R CraftManager/ /home/sujimichi/KSP/dev_KSP-1.4.3/GameData/CraftManager/

#rm -rf /home/sujimichi/Share/KSP/CraftManager/
#cp -R CraftManager/ /home/sujimichi/Share/KSP/CraftManager/
