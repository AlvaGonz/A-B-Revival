package f4.unit.parts
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.unit.fire.FirePorts;
   import f4.unit.move.MoveAs;
   import f4.unit.render.Render;
   import f4.unit.upgrade.FlyData;
   import fullshit.ds.AbsOne;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class FlyBase extends AbsOne
   {
      
      public var render:Render;
      
      public var firePorts:FirePorts;
      
      public var move:MoveAs;
      
      public var data:FlyData;
      
      public var targets:Targets;
      
      private var _rect2:Rect2;
      
      public function FlyBase(param1:MoveAs)
      {
         super();
         this.targets = InitData.TARGETS;
         this._rect2 = StaticShit.RECT2;
         this.move = param1;
      }
      
      public function fireRun() : void
      {
         this.move.runMove();
         this.render.runRender(0);
      }
      
      override public function runA1() : void
      {
         this.render.runRender(0);
         this.firePorts.runPorts(true,this.data.fireacc,null);
         this.outCam();
      }
      
      public function outCam() : void
      {
         if(this.move.point.x < -300 || this.move.point.x > this.render.cam.wh.x + 300 || this.move.point.y < -300 || this.move.point.y > this.render.cam.wh.y + 300)
         {
            isDeadA1 = true;
         }
      }
   }
}

