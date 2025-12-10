# Carts

## Building

Clone or download the project and place it on the path ```~\RimWorld\Mods``` . Open the project in visual studio. The solution file was created using Visual Studio 2022.

In your Solution Explorer (the panel usually located on the right), right click your project -> Properties (or expand your project and double click "Properties" with the wrench icon) and make sure Framework is ".NET Framework 4.7.2"

Go to Build and make sure the Output Path is ```..\Assemblies\```  (Or wherever the Assemblies folder is)

Delete invalid references
``` 
Assembly-CSharp.dll
UnityEngine.CoreModule.dll
 ```
And then re-add references
1. Expand your project in Solution Explorer. Then right click "References" -> Add Reference...
2. Click Browse...
3. Navigate towards
  ```RimWorldInstallPath/RimWorld******_Data/Managed ```
  and select files above.
4. Click "Add"
5. Click "OK" to close the Reference Manager.
6. Right-click on both Assembly-CSharp.dll and UnityEngine.dll and set Copy Local to False (Properties pane).

The solution also has a dependency on the following third-party DLL:
``` 
0Harmony.dll
``` 
The Harmony DLL is available from https://github.com/pardeike/Harmony/releases/. Must be installed as a rimworld mod. And the project must contain a link to the dll.