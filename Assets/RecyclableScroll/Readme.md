### **Quick Setup Guide**

1. Attach the`RecyclableScrollVertical.cs` / `RecyclableScrollHorizontal.cs` script to the *GameObject* containing the *ScrollRect* (you can place it elsewhere too 😅).
2. Drag the`ScrollRect` and `ContentRT` variables for `RecyclableScrollVertical.cs` / `RecyclableScrollHorizontal.cs` onto the Inspector window. Set the maximum number of list items to display in `TotalItems` (or set it via code). Other variables visible in the Inspector will update automatically.
3. Configure the UI items in the list as child objects of the *Content* game object (make sure the number is just enough to fill the viewport).
4. Configure `RecyclableScrollVertical.cs` / `RecyclableScrollHorizontal.cs` using the `Init()` function with a callback function as a parameter to update the data. See the sample script `ItemLoader.cs` and the sample scene *Scene_RecyclableScroll* in*Samples*.
5. Retrieve the list of UI items in*the Content* using the `TryGetComponentsInContentChildren()`  function. See the sample script`ItemLoader.cs`and the sample scene *Scene_RecyclableScroll* in*Samples*.


[Doc](https://luong-tho-thanh-son.notion.site/Recyclable-Scroll-Reusing-UI-Elements-6cbcbf2db373823ab6e381fb95a9da54)