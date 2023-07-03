# Power Grid Inventory - Again!  
PGIA is revamp of the classic PGI. A diablo style, grid based inventory system for Unity.

# What's New  
A remake of PGI Classic, this new inventory system has been redesigned from scratch to take advantage of all the
features Unity brings to the table in 2022+. Such things include: newer C# language features, the use of scriptableobjects 
and both MonoBehaviour and non-MonoBehaviour based data models, and Unity's new GUI system, UIToolkit.

# Heads Up  
Take heed however, this is not going to be a commercial product like before and as such I will not be taking feature requests or providing official
support. It is designed with my own personal projects in mind and is subject to change as needed. But given that I know of a handful of people that
have been dying for PGI to see new light, I have decided to make it public for anyone to use, branch, and expand upon.

This project absolutely requires Odin. I do not have any desire to write custom editors when such a tool exists. If you do not have access to Odin
you can still theoretically use PGIA but it will require you to roll your own property drawers that match Odin's drawer attributes in order to gain
the full functionality. It also requires my own HashedString datatype which is linked below.

# Dependencies  
[Hashed String](https://github.com/Slugronaut/Toolbox-HashedString)  
[Odin](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)  

<br />
<br />

# A Simple Guide to Using PGIA 
The following is a quick-start guide for working with PGIA. It will not go into every detail but will instead provide the very basics to get an example up and running. Luckily once you understand the fundamentals of how to setup the rest shouldn't been too hard to explore and learn on your own. Under the hood it is a rather simple system.

<br />
<br />

## Environment setup  
<br />

### IElements / UIToolkit  
PGIA is designed to work with Unity's new UIToolkit and does not support the old UGUI system anymore. You can ensure it is active by going to the Package Manager, selecting the 'Built-in' packages from the dropdown and searching for UIElements. You should see something like the following screenshot if it is enabled.  

![](<doc/images/example01.png>)  

### Input System  
PGIA also requires the new Input System from Unity in order to properly track mouse positions. Unfortunately, some parts of UIElements still rely on the old system as well but luckily Unity supports both system simultaneously. Once again you can use the package manager to ensure you have installed the new Input System.  
![](<doc/images/example02.png>)  

After installing Unity will likely ask you if you want to enable the new system and then require a restart. You should opt to do this. You can also toggle this option in the *Project Settings->Player->OtherSettings* and set *Active Input Handling* to *Both*.

### Odin
Next up you will require Odin's Inspector tools. Anyone that is truly serious about developing with Unity should already have Odin Inspector and Serializer at this point but if for some reason you do not it will be required to continue. You can visit their website to learn more and even try out a free demo. [https://odininspector.com/download](https://odininspector.com/download)  

### Hashed String
And finally, you'll need a small library of my own. It simply allows you to decalre a HashedString datatype that can be viewed in the inspector and serialized by Unity. Get it here. [HashedString](https://github.com/Slugronaut/Toolbox-HashedString)  

<br />
<br />

## Setting Up the Root UI
The first thing you'll want to do is define some assets that will be used for your system. Start up the UIBuilder and create a new asset. We'll call it UIPanel for now. Add a *VisualElement* to it. Provide a simple background color for it. Next, add another *VisualElement* as a child of the first and again give it a background color. This way we'll be eable to see what is going on.  

The child element should also be given a name. In this example let's call it 'GridContainer'. This will be used to contain your grid of cells that will be generated.  

Here is an example of how things might look for our example.  

![](doc/images/example03.png)  


After that, add create an empty GameObject in your scene and add a UIDocument component to it. Then supply a link to your UIPanel.  

![](doc/images/example04.png)  



## More UIElements  
So we'll need a couple more UI elements before we can get properly started. These will define both our cursor and the actual cells of the grid. Don't worry, I promise this will be easy. Let's start with the cursor.  

### The Cursor
Open the UIBuilder again and add a single *VisualElement*. Name it 'Cursor'. This is important as this will be the tag that PGI uses to locate this particular object. Next, go to the Attributes and be sure to set *Picking Mode* to *Ignore. This is done in code by PGIA but on some versions of Unity the API doesn't work so it must be forced manually in the editor .Save the file as somthing like Cursor.uxml and that's it! All done!  

![](doc/images/example05.png)  

See? I told you it would be easy. This particular element you created will be used to display the icon of the item being dragged around in the inventory. As such it will dynamically be resized as needed. You *can* add additional styling if you really want to but it's not necessary at all. When you aren't dragging anything it will remain invisible.  

### The Cursor Screen
There may be some cases where we want to have multiple different UIs on the screen but there could be a gap between them. For example a shop might have two windows on either side of the screen but nothing inbetween them. UIToolkit has no way of displaying or tracking movement across these so we'll need a kind of dummy object to handle this for us.

In the UIBuilder create a new tree. You can leave this one empty as PGIA will handle everything else internally. All you need to do is go to *Canvas Size* on the root and check the box for *Match Game View*.  

![](doc/images/example06.png)  


### The Grid Cell  
The final element is the definition of the cell used by the grid. This is probably the most complicated. In short you need to create yet another element in UIBuilder, add the PGI.uss stylesheet from the PGIA Package UI folder. Then create a *VisualElement* and name it 'Container' and apply the *.GridSlot* stle class to it. Likely you'll want Next add a text element as a child of Container and name it 'StackQty'.  

After that you'll either want to manually override the border of the Container element to seomthing like 2px with a color you like or alternatively you could define the variables used by the PGI.uss style sheet. They are as follows:  

```
border-left-color: var(--pgia-theme-cellborder-prime);  
border-right-color: var(--pgia-theme-cellborder-prime);  
border-top-color: var(--pgia-theme-cellborder-prime);  
border-bottom-color: var(--pgia-theme-cellborder-prime);
```
  
![](doc/images/example07.png)  


As with all of the other elements you can choose to add any other number of visual flourishes you desire including the placement of the StackQty element. This is all rather long-winded though so if you prefer a shortcut you can simply copy the GridCell.uxml file found in PGIA's UI folder and start there, You'll still need to either define the border variables or override them in the UIBuilder yourself.

<br />
<br />

## ScriptableObject Assets  
There are a number of assets that are used by PGIA to share data between instances of objects. Let's create them now. We'll start with the DragCursor asset.

### Drag Cursor Asset  
First let's create a Drag Cursor Asset. This will contain information that is referenced by PGIA when displaying the the icon for an item when it is being moved around.  

In your project window create a folder to hold all of your UI assets and then open the context menu and navigate to *Create->PGIA->Drag Cursor*. This will create a DragCursor scriptableobject asset in that folder.  

Click on the new file and you'll see three entries. The first one is called 'Cursor Asset'. Locate the Cursor.uxml file you created earlier and drag it into that spot. Next you'll see 'Screen Asset'. Do the same with the 'CursorScreen.uxml' file you created earlier. This will allow any view using this DragCursor asset to know how to actually display it.  

The final entry is called 'Panel Settings'. If you look at the UIDocument component in your scene that you created earlier you'll see a refernce to a 'PanelSettings' asset. Locate that file and then make a duplicate of it and name it 'CursorSettings'. Select that asset and then change *ScaleMode* to *Scale With Screen Size*. You'll also likely want o set a reference resolution and slide the *Match* bar all the way to *Height*. Finally, like this asset to the *Panel Settings* of your DragCusror. Setting the scaling is important here because it ensures that the mouse clicks and drag icon are registered in the correct location of the screen regardless of your screen resolution or ratio.  

When you are don it should look like the below images.  

![](doc/images/example08.png)  

![](doc/images/example09.png)  

### Grid View Asset  
Next we need an asset that defines some of the visual aspects of our grid views. Again navigate to your UI folder and use the conext menu to select *Create->PGIA->Grid View Asset*. For now you can leave this file with its default values. Which is what we will work on next.  

<br />
<br />

## MVC
PGIA uses a very simple form of MVC to manage data and display it. A Model simply stores the state of an inventory. A view can be given a link to a model and it will display that model's state as a UI grid. The model can even be dynamically changed to allow the reuse of a single view.  

For the most part the view handles updating of the model when the user interacts with its UI. And if the model is updated manually through code it will inform the view and cause it to reflect any changes. Which means you can mostly just write code to interact with the model and it will just work.  


>NOTE:  
PGIA provides a GridModel and IGridModel interface that allows for creating    inventories entirely through code. However there is a convience MonoBehaviour called GridModelBehaviour that simply wraps a GridModel to allow for easier integration with Unity's inspector. The GridViewBehaviour currently is not a wrapper but instead the actual view object. This may change in the future.


### The Model
Create a new GameObject in the scene and name it 'Inventory'. Attach a *GridModelBehaviour* to it. This will represent the data of our inventory. Give it a size of 10x10 cells. The other options can be left alone for now. Congrats! You now have an inventory!

![](doc/images/example10.png) 


### The View
Locate the GameObject that has your *UIDocument* component and add a *GridViewBehaviour*. It's not necessary that it be attached to the same object as the UIDocument but I find it usually helps with organizing things since they are very closely related.  

Now it's time to glue everything together! First link the *Model* to the *GridModelComponent* you just created in the previous step. Next link the *View* to the *UIDocument* component int his same GameObject. Be sure the *Grid Container Id* matches the name you gave to the container element in your UI. It should be *GridContainer* if you followed along exactly.  

> This value can use dot separators to find child elements. By default it will search out the first valid entry it can find but if you have a complicated structure you can specify a hirarchy.  

>For example you could type 'Panel.GridContainer' if you also named the parent element 'Panel'. This allows you to have multiple containers with the same name that can be linked seperately based on their hierarchy in the UI tree.  

Next up, link the *Cursor Asset* to the Drag Cursor asset we created earlier. Then link the *Cell UI Asset* entry to the GridCell.uxml we created. Finally, link the *Shared Grid Asset* to the Grid View Asset and we are finally done! If you followed along closely it should like similar to this:  

![](doc/images/example11.png) 


And with that, you are all done! You should now have a working inventory and view setup and ready to go! Try entering play mode and you should see a grid populating your UI.

![](doc/images/example11.png) 

But it looks so empty. Let's make some items and learn how to fill it.


## Inventory Items

### Not Yet Written...



