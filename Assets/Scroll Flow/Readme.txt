Hello, Thanks for bying our asset!

This asset contains a wonderful scroll mechanic for TextMeshProUGUI (you can easily modify it for default text if you want)
with full customisation like shape of scroll, sensibility, size, ...
Also, we included our own layout group mechanic, which works much better than Unity one.


How to use:

In folder Scroll Flow/Prefabs you can find ScrollSystem object with script ScrollMechanic, which do the trick.
You simply need to drag this prefab in scene! To test it properly, you could modify array TestData, bool IsInfinite to select scroll type and activate InitTest
in runtime. Also, to make it work, you should apply your camera to variable Camera and you canvas to TargetCanvas and when you press Start - 
you will see a scrolling of your TestData lines, which shape will depend on ScrollSystem height and several variables in script.
To initialise scroll, you should use method Initialize (in script you could see full description about it).
Also you can get current selected text by using function GetCurrentValue.

Now, about settings:

HeightTemplate is a height of a template text object. It's only variable you should set before start.
Curve is a shape of a scrolling and CurveShift is a curve of text offset. They can be edited in runtime to control and define any shape you need.
SpeedLerp is a speed of concentration on current text.
MinVelocity is a minimun inertia value to start concentrate on current text.
ShiftUp and ShiftDown are using to controll text offset. You also can edit them in runtime.
Padding is a spacing from upper and lower border of ScrollSystem object.
ColorPad is a setting of text fade. To got how it works - you can edit it in runtime.
MaxFontSize is a size of template text font.
IsElastic makes scroll elastic in nonInfinity type.
MaxElastic is maximum elasity distance.
InertiaSense is a speed of inertia fading.

If you want to use our own layout group - you should read a comments in script AutoSizeLayout


Contacts:
Email: korobko416@gmail.com
Telegram: @Alexandr_Korobko