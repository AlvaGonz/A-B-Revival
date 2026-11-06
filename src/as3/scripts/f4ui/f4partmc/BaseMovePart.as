package f4ui.f4partmc
{
   import f4.data.InitData;
   import f4.unit.render.DisDown;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.display.Stage;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.geom.Point;
   
   public class BaseMovePart
   {
      
      public var mc:MovieClip;
      
      public var follow:Boolean;
      
      public var parent:DisplayObjectContainer;
      
      public var stage:Stage;
      
      public var startxy:Point;
      
      public function BaseMovePart(param1:MovieClip)
      {
         super();
         this.mc = param1;
         this.follow = false;
         this.parent = this.mc.parent;
         this.stage = this.mc.stage;
         this.startxy = new Point();
         this.mc.addEventListener(MouseEvent.MOUSE_DOWN,this.mouseClick);
         this.mc.addEventListener(Event.ENTER_FRAME,this.loop);
         this.mc.addEventListener(MouseEvent.MOUSE_OVER,this.mouseOver);
         InitData.UICTRL.disA1.addOne1(new DisDown(this.mc));
      }
      
      public function mouseOver(param1:MouseEvent) : void
      {
      }
      
      public function stageClick(param1:MouseEvent) : void
      {
      }
      
      public function mouseClick(param1:MouseEvent) : void
      {
         this.follow = !this.follow;
         if(this.follow)
         {
         }
      }
      
      public function loop(param1:Event) : void
      {
         if(this.follow)
         {
         }
      }
      
      public function startFollow() : void
      {
         this.startxy.x = this.mc.x - this.parent.mouseX;
         this.startxy.y = this.mc.y - this.parent.mouseY;
      }
   }
}

