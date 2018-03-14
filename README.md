# RSDrawGraphics
This is a RobotStudio Smart Component to draw graphics defined in RAPID code.

You can find in modGfxShapeData.sys some definitions to calculate collision with these data.
In MainModule.mod, you have an example how to use it.
Only add these two modules in your controller, then create a component linked to GfxShapeData.

## What you have to do before compiling:
  - Update ABB.Robotics.* References to Good RobotStudio SDK Version path with ***Project*** - ***Add Reference*** - ***Browse***.
  - On Project Properties:
    - **Application**: Choose good .NET Framework version.
    - **Build Events**: *Post Build Events*: Replace with the good LibraryCompiler.exe Path.
    - **Debug**: *Start External Program*: Replace with the good RobotStudio.exe Path `This not work if project on network drive, let it clear.`
  - In *\RSDrawGraphics\RSDrawGraphics.en.xml*:
    - Replace **xsi:schemaLocation** value with good one.
  - Same for *\RSDrawGraphics\RSDrawGraphics.xml*.

### If your project path is on network drive:
##### To get RobotStudio load it:
  - In *$(RobotStudioPath)\Bin\RobotStudio.exe.config* file:
    - Add in section *`<configuration><runtime>`*
      - `<loadFromRemoteSources enable="true"/>`

##### To Debug it:
  - Start first RobotStudio to get RobotStudio.exe.config loaded.
  - Then attach its process in VisualStudio ***Debug*** - ***Attach to Process..***
      
## Usage
![RSDrawGraphics](https://raw.githubusercontent.com/DenisFR/RSDrawGraphics/master/RSDrawGraphics/RSDrawGraphics.jpg)
### Properties
  - ***Controller***:\
The Controller to get data. (Simulated or Real if connected)
  - ***Task***:\
The Task to get data.
  - ***Module***:\
The Module to get data.
  - ***Data***:\
The ShapeData data.
  - ***ShapeType***:\
Capsule or Box
  - ***ParentAxis***:\
Axis where shape is.
  - ***CurrentAxis***:\
Current position for Axis.(Must be updated by controller)
  - ***Shift***:\
Shift position on Axis.
  - ***Radius***:\
Radius.
  - ***Length***:\
Length.
  - ***Height***:\
Height.
  - ***Width***:\
Width.
  - ***Color***:\
Color [Red,Green,Blue] (0-255).
  - ***Opacity***:\
Amount to blend with the object's original color (0-255).
  - ***ShowAtOrigin***:\
Set to true to show shape at Origin.
  - ***MakeAxisFrame***:\
Set to true to make frame at Axis Position.
  - ***IsSelectable***:\
Enable part to be selected in the Graphics window.
### Signals
  - ***UpdateGraph***:\
Set to high (1) to update the generated part when new values applied.
  - ***SyncToRAPID***:\
Set to high (1) to update the RAPID code with current values.(Auto-resetted at end of Sync)\
If no Data is selected, the value are copied in clipboard.\
You just have to paste it in your module.

### Events
This SmartComponent is connected to LogMessage.\
When a specific log comes from his controller, it update itself.
  - If message contains:
    - "Update RSDrawGraphics"
    - "Checked:" ... "No Errors." (Translated for all RS languages)
  - When Program stop (10122)
  - When Program restart (10124)