## Change Log

###Versioning:
The Major version number will only change if there is an API change on KerbalX which would make previous versions incompatible.
The Minor version number is (a bit unconventionally) being used to indicate compatibility with KSP versions.
The Patch version number is incremented with each update.  ie 1.0.2 & 1.1.2 have the same features but are for different versions of KSP
**1.0.x are builds for KSP 1.3.x | 1.1.x are builds for KSP 1.4.x**


#### 1.0.3 & 1.1.3
**Change:** The primary name shown is now the internal ship name, and (if different) the file name is shown in smaller font.  
**Change:** Renaming craft now allows non OS safe chars to be used in the internal name, but those chars will be replaced with an underscore in the filename. [**note**, may break some tag associations].  
**Added:** Group Actions; You can now select multiple craft (hold ctrl while selecting, hold shift to select all craft between two craft, and ctrl+a to select all) and then perform actions across all of them; add/remove Tags, Transfer between craft type [SPH/VAB/Subs], Move/Copy between saves and Delete.  RHS panel shows info for combined mass, cost and crew capacity when a group is selected.  
**Added** QuickTag; lets you add/remove tags to the current craft in the editor without having to open the main Craft Manager interface. click the tags icon in the toolbar or press ctrl+t to open it (esc or ctrl+t again will close it). Open it, add/remove a tag, done (changes are saved right away).  
**Added:** Height of main UI can be adjusted in settings.  
**Added:** When updating a description via CM if the selected craft matches the currently loaded craft the stock description field will be updated too. (note if multiple craft share the same name this won't happen as there's no way to tell which craft is loaded).  
**Added:** There is the option in settings to have a larger thumbnail shown in the details panel (disabled by default).  
**Tweak:** Thumbnail icon is slightly bigger.  
**Tweak:** Autosaved-Craft has a highlight in the list to make it more distingusable from the others.  
**Tweak:** If the cache was genererated by another version of CM will be reset.  
**Tweak:** If 1 or more craft are selected, clicking to drag the list will no longer deselect the selected craft.  
**Fix:** version for KSP 1.4.x was not detecting ctrl + click (previously used for selecting multiple craft type filters, now also used in group selecting craft).  
**Fix:** Fixed issue where moving a craft between saves would fail unless the craft had a thumbnail already generated.  


#### 1.0.2 & 1.1.2
**Fix:** issue caused when creating list of installed parts when there are more than 1 parts with the same name.
**New:** CM can use KerbalX to lookup which mods missing parts on a craft belong to.

#### 1.0.1 & 1.1.1  
minnor bug fixes:  
Ensure ship/subassemblies folders exist before downloading craft  
Autopopulate new save wasn't offering most recent craft, fixed  
Download queue notification wasn't being cleared once craft had downloaded, fixed  


#### 1.1.0  
Initial full release for KSP 1.3.x

#### 1.0.0  
Initial full release for KSP 1.3.

