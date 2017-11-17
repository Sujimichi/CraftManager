#Collie - rounds all the files up into release

rm -rf bin/Release/CraftManager
rm bin/Release/CraftManager.zip

mkdir bin/Release/CraftManager -p
mkdir bin/Release/CraftManager/Plugins -p

cp bin/Release/*.dll bin/Release/CraftManager/Plugins/

cp -a Assets/*.* bin/Release/CraftManager/
cp LICENCE.txt bin/Release/CraftManager/LICENCE.txt

#ruby -e "i=%x(cat Source/KerbalX.cs | grep version); i=i.split('=')[1].sub(';','').gsub('\"','').strip; s=\"echo 'version: #{i}' > bin/Release/KerbalX/version\"; system(s)"


rm bin/Release/*.dll
rm bin/Release/*.dll.mdb

cd bin/Release
zip -r CraftManager.zip CraftManager/


rm -rf /home/sujimichi/KSP/dev_KSP-1.3.1/GameData/CraftManager/
cp -R CraftManager/ /home/sujimichi/KSP/dev_KSP-1.3.1/GameData/CraftManager/
