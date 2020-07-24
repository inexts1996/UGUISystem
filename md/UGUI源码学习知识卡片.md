# ugui 知识卡片写作

* image设置新的sprite，会调用graphic里的setalldirty方法，对layout、vertical以及material进行脏处理设置。脏处理的过程就是将这些ui元素注册到canvasupdateregistry当中，添加到对应的layout以及graphic队列中，进行rebuild。需要注意的是，layout的rebuild对象主要是继承自实现了icanvaselement的layoutrebuilder对象，需要进行layout更新的，会在获得一个layoutrebuilder对象后进行自己的初始化设置，之后注册到canvasupdateregistry，在里面进行update和rebuild，其实最后还是调到layoutrebuilder的rebuild方法，进行layout的rebuild，也就是对元素进行水平和垂直方向的大小计算，之后重新进行设置

### graphic类

**attribute**：不允许同一组件出现多个；依赖canvasrenderer和recttransform组件；在play mode和editor mode下都可以执行

[disallowmultiplecomponent]

[requirecomponent(typeof(canvasrenderer))]
[requirecomponent(typeof(recttransform))]
[executealways]

继承：uibehaviour、icanvaselement

子类：maskablegraphic

涉及到的类：

1. layoutrebuilder，主要用来进行layout rebuild的操作
2. canvasupdateregistry，主要用来进行canvas的更新操作，所有的操作都是在这里进行的，包括layout的rebuild
3. graphicregistry，服务于 **graphicraycaster** 主要用来将canvas与其下的元素进行关联，用于**raycast**射线检测

icanvaselement类最主要的方法就是rebuild，供子类实现对应的重建操作。

uibehaviour继承自monobehaviour，主要提供一些unity的声明周期函数，以及一些ui更新相关的方法

```c#
    //recttransform尺寸发生改变时执行
    protected virtual void onrecttransformdimensionschange()
    {
    }
    //在父节点发生改变之前调用
    protected virtual void onbeforetransformparentchanged()
    {
    }
	//父节点发生改变时调用
    protected virtual void ontransformparentchanged()
    {
    }
	//当动画发生改变时执行
    protected virtual void ondidapplyanimationproperties()
    {
    }
	//当canvasgroup发生变化时执行
    protected virtual void oncanvasgroupchanged()
    {
    }

    /// <summary>
    /// called when the state of the parent canvas is changed.
    /// </summary>
    //当canvas active或者enable发生改变时执行
    protected virtual void oncanvashierarchychanged()
    {
    }
```
在graphic的onenable函数中，会首先执行**cachecanvas**，查找当前image、text或者rawimage所在的canvas，存储在m_canvas中。

```c#
private void cachecanvas()
        {
            var list = listpool<canvas>.get();
            gameobject.getcomponentsinparent(false, list);
            if (list.count > 0)
            {
                // find the first active and enabled canvas.
                for (int i = 0; i < list.count; ++i)
                {
                    if (list[i].isactiveandenabled)
                    {
                        m_canvas = list[i];
                        break;
                    }
                }
            }
            else
                m_canvas = null;

            listpool<canvas>.release(list);
        }
```

**cachecanvas**函数除了会在onenable的时候执行之外，还会在**oncanvashierarchychanged（canvas显示隐藏或者onenable的时候执行）**、**ontransfromparentchanged（父节点发生改变的时候，也会重新查找当前的canvas）**最后就是属性canvas中，每次使用属性canvas时，都会调用一下

之后在onenable中会调用 **graphicregistry类的registergraphicforcanvas**函数，将当前元素与它所在的canvas关联起来，存放到 **m_graphic**字典中

```c#
public static void registergraphicforcanvas(canvas c, graphic graphic)
        {
            if (c == null)
                return;

            indexedset<graphic> graphics;
            instance.m_graphics.trygetvalue(c, out graphics);

            if (graphics != null)
            {
                graphics.addunique(graphic);
                return;
            }

            // dont need to addunique as we know its the only item in the list
            graphics = new indexedset<graphic>();
            graphics.add(graphic);
            instance.m_graphics.add(c, graphics);
        }
```

然后调用 **setalldirty**对材质、layout以及顶点进行脏处理设置

```c#
public virtual void setalldirty()
        {
            setlayoutdirty();
            setverticesdirty();
            setmaterialdirty();
        }
```

## graphic源码解析之layout rebuild
其中layout的脏处理设置，会调用 **layoutrebuilder**进行标记处理

```c#
public virtual void setlayoutdirty()
        {
            if (!isactive())
                return;

            //添加到布局重新build的对象池中
            layoutrebuilder.marklayoutforrebuild(recttransform);

            if (m_ondirtylayoutcallback != null)
                m_ondirtylayoutcallback();
        }
```

marklayoutforrebuild方法会查找当前物体最上层带有ilayoutgroup组件并且处于激活状态的父级节点。如果当前节点就是查找到的layoutroot，会对当前节点进行layoutcontroller的检测，如果不存在可用的layoutcontroller组件，则直接返回。否则进行layoutroot重新build的标记

```c#
public static void marklayoutforrebuild(recttransform rect)
        {
            if (rect == null || rect.gameobject == null)
                return;

            //从对象池中pop一个component类型的list
            var comps = listpool<component>.get();
            bool validlayoutgroup = true;
            recttransform layoutroot = rect;
            var parent = layoutroot.parent as recttransform;
            while (validlayoutgroup && !(parent == null || parent.gameobject == null))
            {
                validlayoutgroup = false;
                //获取当前父物体上所有继承自ilayoutgroup的组件列表
                //scrollrect、gridlayoutgroup、verticallayoutgroup、horizontallayoutgroup
                parent.getcomponents(typeof(ilayoutgroup), comps);

                for (int i = 0; i < comps.count; ++i)
                {
                    var cur = comps[i];
                    if (cur != null && cur is behaviour && ((behaviour) cur).isactiveandenabled)
                    {
                        //找到并且处于激活状态直接返回，validlayoutgroup设为true，保留当前物体
                        validlayoutgroup = true;
                        layoutroot = parent;
                        break;
                    }
                }

                //继续向上查找，直到找到最顶层包含继承自ilayoutgroup组件的root，进行统一的布局
                parent = parent.parent as recttransform;
            }

            // we know the layout root is valid if it's not the same as the rect,
            // since we checked that above. but if they're the same we still need to check.
            //如果当前rect就是根节点的话，判断是否有可用的layoutcontroller可用
            //有的话，就走根节点layout重新build的方法
            if (layoutroot == rect && !validcontroller(layoutroot, comps))
            {
                //用完之后，将list回收，供以后使用
                //这样一个类型的list可以重复被利用，除非个数不够了，需要重新创建，其余时间重复利用就ok了，牛逼
                listpool<component>.release(comps);
                return;
            }

            marklayoutrootforrebuild(layoutroot);
            listpool<component>.release(comps);
        }
```

根节点的标记，本质就是注册到canvasupdateregistry中去, **tryregistercanvaselementforlayoutrebuild**只接受继承自 **icanvaselement**的参数，而s_rebuilders就是一个layoutrebuilder的池子，layoutrebuilder继承自icanvaselement

```c#
private static void marklayoutrootforrebuild(recttransform controller)
        {
            if (controller == null)
                return;

            var rebuilder = s_rebuilders.get();
            rebuilder.initialize(controller);
            //如果注册rebuild队列中已经包含了这个gameobject，就直接进行释放回收
            //不包含的话，就注册进去，暂时不回收
            // 19/5 2020 image 源码学习
            //这里注册的到rebuild队列的对象类型为继承自icanvaselement的layoutrebuilder
            //主要是受s_rebuilders的影响,这样在canvasupdateregistry中进行layoutrebuid的时候调用的就是layoutrebuilder重写的rebuild方法了
            //tryregistercanvaselementforlayoutrebuild这个方法使用了多态的概念
            if (!canvasupdateregistry.tryregistercanvaselementforlayoutrebuild(rebuilder))
                s_rebuilders.release(rebuilder);
        }
```

通过**tryregistercanvaselementforlayoutrebuild**注册之后，将会在canvasupdateregistry这个单例类的 **performupdate**方法中，调用m_layoutrebuildqueue中对象元素的rebuild方法。

```c#
private void performupdate()
        {
            uisystemprofilerapi.beginsample(uisystemprofilerapi.sampletype.layout);
            cleaninvaliditems();

            // 18/5 2020 image源码学习
            //标记当前正在执行layout的rebuild
            //避免此时注册到队列中或者从队列中移除
            m_performinglayoutupdate = true;

            m_layoutrebuildqueue.sort(s_sortlayoutfunction);
            for (int i = 0; i <= (int) canvasupdate.postlayout; i++)
            {
                for (int j = 0; j < m_layoutrebuildqueue.count; j++)
                {
                    // 18/5 2020 image源码学习
                    //从队列中取得一个element
                    var rebuild = instance.m_layoutrebuildqueue[j];
                    try
                    {
                        // 18/5 2020 image源码学习
                        //element可用的情况下进行rebuild,这里调用基本上是layoutrebuilder中的rebuild方法
                        if (objectvalidforupdate(rebuild))
                            rebuild.rebuild((canvasupdate) i);
                    }
                    catch (exception e)
                    {
                        debug.logexception(e, rebuild.transform);
                    }
                }
            }
    .........
}
```



相当于针对每一个需要layoutrebuild的元素，分配一个layoutrebuilder对象 （从s_rebuilder中获取的）。所以在canvasupdateregistry类的performupdate方法中，调用的其实就是layoutrebuilder的rebuild方法，
这里需要传入一个 **canvasupdate**的枚举值，但只有当layout的时候，才会进行重新的build。主要是重新对水平和垂直方向的layout重新进行计算，然后分别进行control的设置
```c#
public void rebuild(canvasupdate executing)
        {
            switch (executing)
            {
                case canvasupdate.layout:
                    // it's unfortunate that we'll perform the same getcomponents querys for the tree 2 times,
                    // but each tree have to be fully iterated before going to the next action,
                    // so reusing the results would entail storing results in a dictionary or similar,
                    // which is probably a bigger overhead than performing getcomponents multiple times.
                    performlayoutcalculation(m_torebuild, e => (e as ilayoutelement).calculatelayoutinputhorizontal());
                    performlayoutcontrol(m_torebuild, e => (e as ilayoutcontroller).setlayouthorizontal());
                    performlayoutcalculation(m_torebuild, e => (e as ilayoutelement).calculatelayoutinputvertical());
                    performlayoutcontrol(m_torebuild, e => (e as ilayoutcontroller).setlayoutvertical());
                    break;
            }
        }
```
performlayoutcalculation方法主要是查找需要重新build的元素上继承自ilayoutelement类的组件，调用组件的calculatelayoutinputhorizontal/calculatelayoutinputvertical
计算的过程包含一个递归的。会优先计算子元素的大小，之后再计算父元素，由下向上。
```c#
private void performlayoutcalculation(recttransform rect, unityaction<component> action)
        {
            if (rect == null)
                return;

            var components = listpool<component>.get();
            rect.getcomponents(typeof(ilayoutelement), components);
            stripdisabledbehavioursfromlist(components);

            // if there are no controllers on this rect we can skip this entire sub-tree
            // we don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.count > 0 || rect.getcomponent(typeof(ilayoutgroup)))
            {
                // layout calculations needs to executed bottom up with children being done before their parents,
                // because the parent calculated sizes rely on the sizes of the children.

                for (int i = 0; i < rect.childcount; i++)
                    performlayoutcalculation(rect.getchild(i) as recttransform, action);

                for (int i = 0; i < components.count; i++)
                    action(components[i]);
            }

            listpool<component>.release(components);
        }
```
performlayoutcontrol同样在需要重新build的元素上查找继承自ilayoutcontroller类的组件，调用其方法setlayouthorizontal/setlayoutvertical。设置数据同样存在一个顺序，
由上到下，先设置父级元素，再设置子元素。而且这里针对的是继承自ilayoutselfcontroller（也继承自ilyaoutcontroller）的组件，只有两个组件aspectratiofitter和contentsizefitter。 
```c#
private void performlayoutcontrol(recttransform rect, unityaction<component> action)
        {
            if (rect == null)
                return;

            var components = listpool<component>.get();
            rect.getcomponents(typeof(ilayoutcontroller), components);
            stripdisabledbehavioursfromlist(components);

            // if there are no controllers on this rect we can skip this entire sub-tree
            // we don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.count > 0)
            {
                // layout control needs to executed top down with parents being done before their children,
                // because the children rely on the sizes of the parents.

                // first call layout controllers that may change their own recttransform
                for (int i = 0; i < components.count; i++)
                    if (components[i] is ilayoutselfcontroller)
                        action(components[i]);

                // then call the remaining, such as layout groups that change their children, taking their own recttransform size into account.
                for (int i = 0; i < components.count; i++)
                    if (!(components[i] is ilayoutselfcontroller))
                        action(components[i]);

                for (int i = 0; i < rect.childcount; i++)
                    performlayoutcontrol(rect.getchild(i) as recttransform, action);
            }

            listpool<component>.release(components);
        }
```
ilayoutelement和ilayoutcontroller两个接口，把layoutrebuild这个过程，分成了两部分 ，继承自ilayoutelement的组件只负责重新计算数据，然后通过继承ilayoutcontroller的组件的
方法进行数据的设置。另一个好处就是把水平和垂直的rebuild的过程也独立开来了，一个设置完成再来设置另一个方向。

晚上的时候突然想了一下，感觉graphic里无非就是进行图形的渲染和重建工作的，也就是布局、顶点和材质这三种。完全可以按照这三种方向进行梳理，可能会更加清晰
上面这部分算是基本都是layout rebuild的过程，之后可以再精简一些，到时候用作面试回答。

## graphic源码解析之vertices rebuild
当顶点设置脏处理时，会使用变量m_vertsdirty记录当前顶点处于待更新状态，并且调用canvasupdateregistry的方法registercanvaselementforgraphicrebuild
将当前元素注册到图形rebuild队列中去
```c#
public virtual void setverticesdirty()
        {
            if (!isactive())
                return;

            m_vertsdirty = true;
            canvasupdateregistry.registercanvaselementforgraphicrebuild(this);

            if (m_ondirtyvertscallback != null)
                m_ondirtyvertscallback();
        }
```
m_vertsdirty变量主要是记录当前顶点处于需要更新的状态，主要用在oncullingchanged和rebuild中
oncullingchanged中只有在当前元素父节点挂载rectmask2d组件，并且当前元素的canvasrenderer.cull为false时，
由代码可知，只有在顶点或者材质脏处理的时候，才会注册当canvasupdateregistry中进行图形的重新build
```c#
public virtual void oncullingchanged()
        {
            if (!canvasrenderer.cull && (m_vertsdirty || m_materialdirty))
            {
                /// when we were culled, we potentially skipped calls to <c>rebuild</c>.
                canvasupdateregistry.registercanvaselementforgraphicrebuild(this);
            }
        }
```
rebuild中主要用来在预渲染时判断，如果当前顶点处于脏的状态，则更新几何图形;如果材质脏了，则更新材质
```c#
public virtual void rebuild(canvasupdate update)
        {
            if (canvasrenderer.cull)
                return;

            switch (update)
            {
                case canvasupdate.prerender:
                    if (m_vertsdirty)
                    {
                        updategeometry();
                        m_vertsdirty = false;
                    }

                    if (m_materialdirty)
                    {
                        updatematerial();
                        m_materialdirty = false;
                    }

                    break;
            }
        }
```
之后会把当前元素通过registercanvaselementforgraphicrebuild注册到canvasudpateregistery中的m_graphicrebuildqueue队列中。
registercanvaselementforgraphicrebuild接收一个icanvaselemement的参数,调用它们的rebuild方法进行更新
和layoutrebuild相同，也是在performupdate中循环执行，依次调用每个元素的rebuild方法。unity通过枚举类型canvasupdate，将
layoutrebuild和graphicrebuild分开了。
```c#
private void performupdate()
        {
            ....
            // 18/5 2020 image源码学习
            //graphic rebuild同layout rebuild，只不过没有裁剪操作
            m_performinggraphicupdate = true;
            for (var i = (int) canvasupdate.prerender; i < (int) canvasupdate.maxupdatevalue; i++)
            {
                for (var k = 0; k < instance.m_graphicrebuildqueue.count; k++)
                {
                    try
                    {
                        var element = instance.m_graphicrebuildqueue[k];
                        if (objectvalidforupdate(element))
                            element.rebuild((canvasupdate) i);
                    }
                    catch (exception e)
                    {
                        debug.logexception(e, instance.m_graphicrebuildqueue[k].transform);
                    }
                }
            }

            for (int i = 0; i < m_graphicrebuildqueue.count; ++i)
                m_graphicrebuildqueue[i].graphicupdatecomplete();

            instance.m_graphicrebuildqueue.clear();
            m_performinggraphicupdate = false;
            uisystemprofilerapi.endsample(uisystemprofilerapi.sampletype.layout);
        }
```
这里针对text、image以及rawimage，调用的均为graphic的rebuild方法
```c#
public virtual void rebuild(canvasupdate update)
        {
            if (canvasrenderer.cull)
                return;

            switch (update)
            {
                case canvasupdate.prerender:
                    if (m_vertsdirty)
                    {
                        updategeometry();
                        m_vertsdirty = false;
                    }

                    if (m_materialdirty)
                    {
                        updatematerial();
                        m_materialdirty = false;
                    }

                    break;
            }
        }
```
当元素被剔除的时候，不会进行rebuild。没有被剔除的情况下，只会进行预先渲染。当顶点脏了的时候，
进行几何更新,updategeometry里主要是对mesh的重新生成。
```c#
protected virtual void updategeometry()
        {
            if (uselegacymeshgeneration)
                dolegacymeshgeneration();
            else
                domeshgeneration();
        }
```
domegacymeshgeneration主要是对workmesh进行修改，domeshgeneration则是借助vertexhelper,但是基本操作都是相似的。
已domeshgeneration为例
```c#
private void domeshgeneration()
        {
            if (recttransform != null && recttransform.rect.width >= 0 && recttransform.rect.height >= 0)
                //这里会调用具体子类override的方法
                onpopulatemesh(s_vertexhelper);
            else
                s_vertexhelper.clear(); // clear the vertex helper so invalid graphics dont draw.

            //将顶点列表传入实现了imeshmodifier脚本的组件中去，以便修改mesh
            //实现了imeshmodifier的组件可以修改mesh
            var components = listpool<component>.get();
            getcomponents(typeof(imeshmodifier), components);

            for (var i = 0; i < components.count; i++)
                ((imeshmodifier) components[i]).modifymesh(s_vertexhelper);

            listpool<component>.release(components);

            s_vertexhelper.fillmesh(workermesh);
            canvasrenderer.setmesh(workermesh);
        }
```
onpopulatemesh方法主要是ui元素需要生成顶点的时候调用的，子类元素text、image和rawimage会根据自己的情况override这个方法
向vertexhelper中添加顶点信息,这是graphic中的基本方法。
```c#
protected virtual void onpopulatemesh(vertexhelper vh)
        {
            var r = getpixeladjustedrect();
            var v = new vector4(r.x, r.y, r.x + r.width, r.y + r.height);

            color32 color32 = color;
            vh.clear();
            vh.addvert(new vector3(v.x, v.y), color32, new vector2(0f, 0f));
            vh.addvert(new vector3(v.x, v.w), color32, new vector2(0f, 1f));
            vh.addvert(new vector3(v.z, v.w), color32, new vector2(1f, 1f));
            vh.addvert(new vector3(v.z, v.y), color32, new vector2(1f, 0f));

            vh.addtriangle(0, 1, 2);
            vh.addtriangle(2, 3, 0);
        }
```
顶点信息填充完成之后，查找当前物体上存在的继承imeshmodifier的组件,存在的话会调用imeshmodifier的modifymesh方法，修改mesh的
顶点信息。之后再将s_vertexhelper中的顶点信息填充到workmesh中，最后再传递给canvasrenderer进行设置
vertexhelper就是一个顶点信息处理的工具类。

## graphic源码解析之material rebuild
和顶点更新的逻辑一样，只是最后rebuild中执行的方法不同。调用的是 updatematerial(),更新材质
设置材质的个数，以及材质和贴图
```c#
protected virtual void updatematerial()
        {
            if (!isactive())
                return;

            canvasrenderer.materialcount = 1;
            canvasrenderer.setmaterial(materialforrendering, 0);
            canvasrenderer.settexture(maintexture);
        }
```
还有两个方法，CrossFadeColor和CrossFadeAlpha主要是通过协程完成一个color和Alpha的变换
```c#
public virtual void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha,
            bool useRGB)
        {
            if (canvasRenderer == null || (!useRGB && !useAlpha))
                return;

            Color currentColor = canvasRenderer.GetColor();
            if (currentColor.Equals(targetColor))
            {
                m_ColorTweenRunner.StopTween();
                return;
            }

            ColorTween.ColorTweenMode mode = (useRGB && useAlpha
                ? ColorTween.ColorTweenMode.All
                : (useRGB ? ColorTween.ColorTweenMode.RGB : ColorTween.ColorTweenMode.Alpha));

            var colorTween = new ColorTween
                {duration = duration, startColor = canvasRenderer.GetColor(), targetColor = targetColor};
            colorTween.AddOnChangedCallback(canvasRenderer.SetColor);
            colorTween.ignoreTimeScale = ignoreTimeScale;
            colorTween.tweenMode = mode;
            m_ColorTweenRunner.StartTween(colorTween);
        }
```
发现UI元素的rebuild大概只包含三种，顶点、材质以及布局。而顶点和材质的更新操作则有Graphic来完成，布局则由LayoutRebuilder进行。
二者属于同一等级，都继承自IcanvasElement,最终调用的也就是各自子类实现的rebuild方法。

### maskablegraphic 可被剔除和遮盖的图形
**使用到的类**
* MaskUtilities，提供mask相关的功能方法
* StencilMaterial类主要是用了在图形的基础上，动态的设置自定义材质并且可以正确的清除掉它们

**继承**

​	Graphic、Iclippable、Imaskable、Imaterialmodifier

* Graphic主要就是图形的渲染和rebuild
* IClippable当前元素在继承IClipper接口的物体下时，用来裁剪的。继承IClipper的组件时RectMask2D，也就是当父级带有RectMask2D组件时，子元素可能会发生裁剪。
* IMaskable用来表示当前元素是可以被mask遮盖掉的
* IMaterialModifier用表在CanvasRenderer渲染之前，修改材质用于CanvasRenderer
m_Maskable用来表示当前元素是否可以被遮盖掉。主要用在UpdateClipParent更新父级裁剪以及GetModifiedMaterial获取修改后的材质
```C#
public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                m_StencilValue = maskable ? MaskUtilities.GetStencilDepth(transform, rootCanvas) : 0;
                m_ShouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimisation
            // it adds some coupling between components though :(
            Mask maskComponent = GetComponent<Mask>();
            if (m_StencilValue > 0 && (maskComponent == null || !maskComponent.IsActive()))
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep,
                    CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }

            return toUse;
        }
```
其中MaskUtilities.FindRootSortOverrideCanvas用来查找根节点的Canvas。Canvas的查找过程中，有考虑到
Canvas的嵌套问题，所以对Canvas的overrideSorting有进行判断。当子级Canvas的overrideSorting为true时，
表示子类Canvas的绘制顺序将忽略父级的绘制顺序，可以在顶部或者底部的绘制。所以rootCanvas要么是某一个
overrideSorting为true的子Canvas，要么是最顶层的Canvas。而且只有子级Canvas才会在Inspectors界面出现
overrideSorting选项
```c#
public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvasList = ListPool<Canvas>.Get();
            start.GetComponentsInParent(false, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; ++i)
            {
                canvas = canvasList[i];

                // We found the canvas we want to use break
                if (canvas.overrideSorting)
                    break;
            }

            ListPool<Canvas>.Release(canvasList);

            return canvas != null ? canvas.transform : null;
        }
```
MaskUtilities.GetStencilDepth用来获取当前元素在Mask下的深度值, 这里stopAfter指定的是rootCanvas。
```c#
public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && components[i].MaskEnabled() && components[i].graphic.IsActive())
                    {
                        ++depth;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }

            ListPool<Mask>.Release(components);
            return depth;
        }
```
在之后就是获取当前元素身上是否存在Mask组件，由于存在Mask组件的话，就会生成Mask Material。所以如果当前元素含有Mask组件并且激活的情况下，
就不进行模板材质的添加了。通过的话直接调用StencilMaterial的add方法，获取到一个自定义材质。stencilMaterial的add方法很长，但基本操作就是
先判断当前基础材质是否存在必要的一些属性,都存在的情况下，会根据当前参数列表去m_list中查找缓存下来的自定义材质，存在直接返回。不存在
则进行自定义材质的生成，并存放到m_list中去。并且在材质的使用中，加入了引用计数来进行管理
```c#
public static Material Add(Material baseMat, int stencilID, StencilOp operation,
            CompareFunction compareFunction, ColorWriteMask colorWriteMask, int readMask, int writeMask)
        {
            if ((stencilID <= 0 && colorWriteMask == ColorWriteMask.All) || baseMat == null)
                return baseMat;

            if (!baseMat.HasProperty("_Stencil"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _Stencil property", baseMat);
                return baseMat;
            }

            if (!baseMat.HasProperty("_StencilOp"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _StencilOp property", baseMat);
                return baseMat;
            }

            if (!baseMat.HasProperty("_StencilComp"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _StencilComp property", baseMat);
                return baseMat;
            }

            if (!baseMat.HasProperty("_StencilReadMask"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _StencilReadMask property", baseMat);
                return baseMat;
            }

            if (!baseMat.HasProperty("_StencilWriteMask"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _StencilWriteMask property", baseMat);
                return baseMat;
            }

            if (!baseMat.HasProperty("_ColorMask"))
            {
                Debug.LogWarning("Material " + baseMat.name + " doesn't have _ColorMask property", baseMat);
                return baseMat;
            }

            for (int i = 0; i < m_List.Count; ++i)
            {
                MatEntry ent = m_List[i];

                if (ent.baseMat == baseMat
                    && ent.stencilId == stencilID
                    && ent.operation == operation
                    && ent.compareFunction == compareFunction
                    && ent.readMask == readMask
                    && ent.writeMask == writeMask
                    && ent.colorMask == colorWriteMask)
                {
                    ++ent.count;
                    return ent.customMat;
                }
            }

            var newEnt = new MatEntry();
            newEnt.count = 1;
            newEnt.baseMat = baseMat;
            newEnt.customMat = new Material(baseMat);
            newEnt.customMat.hideFlags = HideFlags.HideAndDontSave;
            newEnt.stencilId = stencilID;
            newEnt.operation = operation;
            newEnt.compareFunction = compareFunction;
            newEnt.readMask = readMask;
            newEnt.writeMask = writeMask;
            newEnt.colorMask = colorWriteMask;
            newEnt.useAlphaClip = operation != StencilOp.Keep && writeMask > 0;

            newEnt.customMat.name = string.Format(
                "Stencil Id:{0}, Op:{1}, Comp:{2}, WriteMask:{3}, ReadMask:{4}, ColorMask:{5} AlphaClip:{6} ({7})",
                stencilID, operation, compareFunction, writeMask, readMask, colorWriteMask, newEnt.useAlphaClip,
                baseMat.name);

            newEnt.customMat.SetInt("_Stencil", stencilID);
            newEnt.customMat.SetInt("_StencilOp", (int) operation);
            newEnt.customMat.SetInt("_StencilComp", (int) compareFunction);
            newEnt.customMat.SetInt("_StencilReadMask", readMask);
            newEnt.customMat.SetInt("_StencilWriteMask", writeMask);
            newEnt.customMat.SetInt("_ColorMask", (int) colorWriteMask);

            // left for backwards compatability
            if (newEnt.customMat.HasProperty("_UseAlphaClip"))
                newEnt.customMat.SetInt("_UseAlphaClip", newEnt.useAlphaClip ? 1 : 0);

            if (newEnt.useAlphaClip)
                newEnt.customMat.EnableKeyword("UNITY_UI_ALPHACLIP");
            else
                newEnt.customMat.DisableKeyword("UNITY_UI_ALPHACLIP");

            m_List.Add(newEnt);
            return newEnt.customMat;
        }
```
之后会将当前自定义的材质m_MaskMaterial从 StencilMaterial中移除掉，主要操作是先进行引用计数的减少，如果当前自定义材质引用次数为0，则立即销毁
自定义材质，并从m_list中移除掉
```c#
public static void Remove(Material customMat)
        {
            if (customMat == null)
                return;

            for (int i = 0; i < m_List.Count; ++i)
            {
                MatEntry ent = m_List[i];

                if (ent.customMat != customMat)
                    continue;

                if (--ent.count == 0)
                {
                    Misc.DestroyImmediate(ent.customMat);
                    ent.baseMat = null;
                    m_List.RemoveAt(i);
                }

                return;
            }
        }
```
然后将新的自定义材质赋值给m_MaskMaterial, 然后赋值给toUse并且返回。这样会拷贝一份新的材质给外界使用，并且不会影响到m_MaskMaterial，牛逼了啊

cull方法，主要用了进行剔除, 重写自IClippable.Cull。主要在RectMask2D中的PerformClipping方法调用，主要是对RectMask2D组件下的子元素进行裁剪。
这里会判断当前元素是否需要剔除，并且调用rect的overlaps来判断当前元素和rootCanvasRect是否存在重叠。不存在重叠的话，就说明当前元素不在rootCanvasRect
内，就可以进行剔除了。也就是两个条件满足一个就可以对当前元素进行剔除操作了
```c#
public virtual void Cull(Rect clipRect, bool validRect)
        {
            //判断是否需要进行剔除
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }
```
之后的UpdateCull才是剔除操作的开始。首先会对比当前cull的状态是否发生改变，改变的话更新当前canvasRenderer的cull状态，并且
调用的cullStateChanged回调。并且执行OnCullingChanged方法
```c#
private void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                canvasRenderer.cull = cull;
                //性能记录
                UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                m_OnCullStateChanged.Invoke(cull);
                OnCullingChanged();
            }
        }
```
OnCullingChanged是调用自父类Graphic，将当前元素注册到CanvasUpdateRegistry中，进行图形Rebuild
```c#
public virtual void OnCullingChanged()
        {
            if (!canvasRenderer.cull && (m_VertsDirty || m_MaterialDirty))
            {
                /// When we were culled, we potentially skipped calls to <c>Rebuild</c>.
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
        }
```
SetClipRect用来设置矩形裁剪区域，以及是否开启canvasRenderer矩形裁剪。开启的情况下，如果当前元素在指定矩形clipRect之外的话
就会进行裁剪，不会再进行渲染。主要是在RectMask2D的PerformClipping以及RemoveClippable方法中调用。
```c#
public virtual void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                //设置rect在canvasRenderered时进行裁剪，当rect超出指定的rect时，会进行裁剪，而不进行渲染
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }
```
MaskableGraphic的OnEnable方法，在开始时会做一些设置。包括重新计算模板裁剪、更新父节点裁剪对象、对材质进行脏处理设置, 而且如果当前
元素存在mask组件，则会通知其下的子元素进行遮罩的重新计算
```c#
protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();

            if (GetComponent<Mask>() != null)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }
```
更新父节点裁剪对象UpdateClipParent, 主要是更新当前元素和最上层父级rectmask的关系。并添和新的rectmask父级元素建立关系
```c#
private void UpdateClipParent()
        {
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;

            // if the new parent is different OR is now inactive
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            if (newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }
```
首先会查找当前元素父级上的rectMask组件。首先是获取处于激活状态的当前元素及其父级元素上的所有rectMask组件，接下来是
对这些RectMask的一个检测。首先检测当前挂载rectMask的物体是否是当前元素，接着判断当前rectMask所在物体是否激活并且当前
组件处于Enable状态。最后则是获取当前元素所在的Canvas物体，判断挂载RectMask组件的物体是否是这些Cavnas的后裔也就是子元素
如果不是，或者当前Canvas是一个子元素并且overrideSorting忽略父级Canvas排序时，则当前RectMask组件作废不能使用。
处于激活状态
```C#
public static RectMask2D GetRectMaskForClippable(IClippable clippable)
        {
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            RectMask2D componentToReturn = null;

            //查找当前元素以及父级元素身上的所有RectMask2D组件
            //包括当前activeSelf=false以及enabled=false的元素
            clippable.rectTransform.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                for (int rmi = 0; rmi < rectMaskComponents.Count; rmi++)
                {
                    componentToReturn = rectMaskComponents[rmi];
                    if (componentToReturn.gameObject == clippable.gameObject)
                    {
                        componentToReturn = null;
                        continue;
                    }

                    if (!componentToReturn.isActiveAndEnabled)
                    {
                        componentToReturn = null;
                        continue;
                    }

                    //查找当前元素所在的Canvas元素
                    clippable.rectTransform.GetComponentsInParent(false, canvasComponents);
                    for (int i = canvasComponents.Count - 1; i >= 0; i--)
                    {
                        //如果当前元素不是这个Canvas或者Canvas的后裔的话，就把componentToReturn置空
                        if (!IsDescendantOrSelf(canvasComponents[i].transform, componentToReturn.transform) &&
                            canvasComponents[i].overrideSorting)
                        {
                            componentToReturn = null;
                            break;
                        }
                    }

                    break;
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);

            return componentToReturn;
        }
```
 RecalculateClipping重新计算裁剪，也是调用的UpdateClipParent方法
 RecalculateMasking重新计算遮罩。首先是将当前材质从模板材质中移除掉，然后置为null，设置重新计算模板为true。最后对材质进行脏处理标记
 ```c#
public virtual void RecalculateMasking()
        {
            // Remove the material reference as either the graphic of the mask has been enable/ disabled.
            // This will cause the material to be repopulated from the original if need be. (case 994413)
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }
```
**子类**
​	text、image、rawimage
RectMask2D是专门用来遮罩继承自MaskGraphic的组件的。
实现了uibehaviour, IClipper, ICanvasRaycastFilter三个接口，其中IClipper接口是专门针对IClippable接口的，用来裁剪继承IClippable的组件的。
这里也就是Image、text和RawImage。
在OnEnable方法中，主要是设置重新计算裁剪的矩形为true，然后将当前rectMask2D元素注册到ClipperRegistry中去，并通知当前元素下的所有子元素
重新进行裁剪的计算。
```C#
protected override void OnEnable()
        {
            base.OnEnable();
            m_ShouldRecalculateClipRects = true;
            ClipperRegistry.Register(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }
```
ClipperRegistry是裁剪的一个辅助类，用来存储挂载有继承自IClipper类的组件的元素。在Cull方法中执行IClipper的PerformClipping方法
```c#
public static void Register(IClipper c)
        {
            if (c == null)
                return;
            instance.m_Clippers.AddUnique(c);
        }
```
Cull方法主要还是在CanvasUpdateRegitry的PerfromUpdate方法中，同layout Rebuild和Graphic Rebuild一起执行的。只不过，cull的部分处于Layout和Graphic
之间，也就是layout -> cull -> Graphic这样的顺序，这样可以剔除和裁剪掉一些不需要渲染的图片,在Graphic阶段节省一些开销。
```c#
public void Cull()
        {
            for (var i = 0; i < m_Clippers.Count; ++i)
            {
                m_Clippers[i].PerformClipping();
            }
        }
```
MaskUtilities.Notify2DMaskStateChanged方法主要是通知其下实现了IClippable接口的组件执行RecalculateClipping进行裁剪的重新计算。
```c#
public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IClippable;
                if (toNotify != null)
                    toNotify.RecalculateClipping();
            }

            ListPool<Component>.Release(components);
        }
```
在OnDisable方法中,做了一些清理裁剪对象以及裁剪者的操作，并且将当前元素从ClipperRegistry中剔除掉，之后通知子类元素进行裁剪的重新计算。
会在编辑器停止运行以及组件Enable设置为false时调用
```c#
protected override void OnDisable()
        {
            // we call base OnDisable first here
            // as we need to have the IsActive return the
            // correct value when we notify the children
            // that the mask state has changed.
            base.OnDisable();
            m_ClipTargets.Clear();
            m_Clippers.Clear();
            ClipperRegistry.Unregister(this);
            MaskUtilities.Notify2DMaskStateChanged(this);
        }
```
m_ClipTargets主要是用来存放被裁剪者的，是在MaskGraphic的UpdateClipParent中调用RectMask2D中AddClippable和RemoveClippable来进行操作的
Add和remove操作无非就是对m_ClipTargets进行添加和移除操作，然后设置当前需要重新计算裁剪矩形，并且是强制裁剪。但是remove的时候重新设置了
一个被裁剪者的裁剪矩形,并且设置当前canvasRenderer.DisableRectClipping(),也就是对当前裁剪元素的裁剪功能进行封禁
```c#
public void AddClippable(IClippable clippable)
        {
            if (clippable == null)
                return;
            m_ShouldRecalculateClipRects = true;
            if (!m_ClipTargets.Contains(clippable))
                m_ClipTargets.Add(clippable);

            m_ForceClip = true;
        }

        public void RemoveClippable(IClippable clippable)
        {
            if (clippable == null)
                return;

            m_ShouldRecalculateClipRects = true;
            clippable.SetClipRect(new Rect(), false);
            m_ClipTargets.Remove(clippable);

            m_ForceClip = true;
        }
```
 IsRaycastLocationValid主要是用来获取当前元素RayCast射线j检测是否可用,可用的状态才会接收射线进行检测。当返回false时，则会忽略当前元素及当前
 元素的下的所有子元素,均不会对其进行射线检测，直接对当前元素下方的物体进行射线检测。
```c#
public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return true;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera);
        }
```
RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera)用来判断相机屏幕内一点是否在rectTransfrom中

 PerformClipping执行裁剪,RectMask最重要的方法。
 ```c#
public virtual void PerformClipping()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // if the parents are changed
            // or something similar we
            // do a recalculate here
            //当父节点发生改变的时候，或者OnHierarchyCanvasChanged以及添加裁剪或者移除裁剪子类元素的时候
            //重新对裁剪的rect进行计算
            if (m_ShouldRecalculateClipRects)
            {
                MaskUtilities.GetRectMasksForClip(this, m_Clippers);
                m_ShouldRecalculateClipRects = false;
            }

            // get the compound rects from
            // the clippers that are valid
            bool validRect = true;
            Rect clipRect = Clipping.FindCullAndClipWorldRect(m_Clippers, out validRect);

            // If the mask is in ScreenSpaceOverlay/Camera render mode, its content is only rendered when its rect
            // overlaps that of the root canvas.
            //这一步是为了剔除子类元素
            RenderMode renderMode = Canvas.rootCanvas.renderMode;
            //如果Canvas的渲染模式ScreenSpaceCamera或者 ScreenSpaceOverlay时，需要裁剪的cliprect与根节点Canvas的rect没有重叠
            //则进行剔除
            //这里好像判断是mask是否被剔除了
            bool maskIsCulled =
                (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.ScreenSpaceOverlay) &&
                !clipRect.Overlaps(rootCanvasRect, true);

            //裁剪区域发生改变
            bool clipRectChanged = clipRect != m_LastClipRectCanvasSpace;
            //是否裁剪的子元素发生变化
            //添加裁剪子元素或者移除时，都为true
            bool forceClip = m_ForceClip;

            // Avoid looping multiple times.
            foreach (IClippable clipTarget in m_ClipTargets)
            {
                if (clipRectChanged || forceClip)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                var maskable = clipTarget as MaskableGraphic;
                if (maskable != null && !maskable.canvasRenderer.hasMoved && !clipRectChanged)
                    continue;

                // Children are only displayed when inside the mask. If the mask is culled, then the children
                // inside the mask are also culled. In that situation, we pass an invalid rect to allow callees
                // to avoid some processing.
                clipTarget.Cull(
                    maskIsCulled ? Rect.zero : clipRect,
                    maskIsCulled ? false : validRect);
            }

            m_LastClipRectCanvasSpace = clipRect;
            m_ForceClip = false;
        }
```
m_ShouldRecalculateClipRects是否需要重新计算裁剪rect，主要是在OnEnable、OnDisable、AddClippable、RemoveClippable以及
 ontransformparentchanged和oncanvashierarchychanged时设置为true。这时在PerformClipping时就需要调用GetRectMasksForClip
 重新获取包含自己在内以及父级元素中带有RectMask2D的元素集合,和方法GetRectMaskForClippable操作类似
 之后通过FindCullAndClipWorldRect方法查找m_Clippers这些RectMask2D中重叠的rect,可用来裁剪
 ```c#
public static Rect FindCullAndClipWorldRect(List<RectMask2D> rectMaskParents, out bool validRect)
        {
            if (rectMaskParents.Count == 0)
            {
                validRect = false;
                return new Rect();
            }

            //逐个查找挂载有RectMask2D重叠部分的rect
            var compoundRect = rectMaskParents[0].canvasRect;
            for (var i = 0; i < rectMaskParents.Count; ++i)
                compoundRect = RectIntersect(compoundRect, rectMaskParents[i].canvasRect);

            var cull = compoundRect.width <= 0 || compoundRect.height <= 0;
            if (cull)
            {
                validRect = false;
                return new Rect();
            }

            Vector3 point1 = new Vector3(compoundRect.x, compoundRect.y, 0.0f);
            Vector3 point2 = new Vector3(compoundRect.x + compoundRect.width, compoundRect.y + compoundRect.height,
                0.0f);
            validRect = true;
            return new Rect(point1.x, point1.y, point2.x - point1.x, point2.y - point1.y);
        }
```
其中RectIntersect返回两个Rect重叠部分的rect
```c#
private static Rect RectIntersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.x, b.x);
            float xMax = Mathf.Min(a.x + a.width, b.x + b.width);
            float yMin = Mathf.Max(a.y, b.y);
            float yMax = Mathf.Min(a.y + a.height, b.y + b.height);
            if (xMax >= xMin && yMax >= yMin)
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            return new Rect(0f, 0f, 0f, 0f);
        }
```
在之后就是获取当前Canvas的渲染模式，如果在ScreenSpaceCamera或者ScreenSpaceOverlay模式下，如果当前元素和
rootCanvasRect没有重叠部分，则当前mask就是被剔除掉了
紧接着就是判断新的裁剪区域是否和上次的裁剪区域相等，赋值给clipRectChanged,标识cliprect发生改变，然后获取是否
需要强制裁剪，赋值给forceClip。
然后遍历所有的被裁剪者m_ClipTargets，裁剪之前会对当前的被裁剪者进行判断，如果不是 MaskableGraphic类型，并且
它的canvasRenderer.hasMoved为false，也就是没有移动过，而且裁剪区域没有改变(clipRectChanged为false），就跳过当前
的被裁剪者。通过的话，就会调用被裁剪者的cull方法
到此，rectMask2D就捋完了。