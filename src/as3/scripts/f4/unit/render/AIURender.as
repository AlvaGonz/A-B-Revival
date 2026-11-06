package f4.unit.render
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.unit.ai.AIPart;
   import f4.unit.particle.Particle;
   import flash.geom.Point;
   
   public class AIURender
   {
      
      public var bodys:Array;
      
      public var others:Array;
      
      public var wings:Array;
      
      public function AIURender()
      {
         super();
         this.bodys = [];
         this.others = [];
         this.wings = [];
      }
      
      public function addPart(param1:AIPart) : void
      {
         if(param1.part.data.type == 0)
         {
            this.bodys.push(param1);
         }
         else if(param1.part.data.type == 1)
         {
            this.wings.push(param1);
         }
         else
         {
            this.others.push(param1);
         }
      }
      
      public function deadOne(param1:AIPart) : void
      {
         if(param1.part.data.type == 0)
         {
            this.bodys.splice(this.bodys.indexOf(param1),1);
         }
         else if(param1.part.data.type == 1)
         {
            this.wings.splice(this.wings.indexOf(param1),1);
         }
         else
         {
            this.others.splice(this.others.indexOf(param1),1);
         }
      }
      
      public function render(param1:Number, param2:Number, param3:Number, param4:Point, param5:Targets, param6:Number) : void
      {
         var _loc7_:Particle = null;
         var _loc8_:Number = NaN;
         var _loc9_:Number = NaN;
         var _loc10_:Number = NaN;
         var _loc11_:AIPart = null;
         var _loc12_:int = 0;
         var _loc13_:int = 0;
         for each(_loc11_ in this.bodys)
         {
            _loc11_.part.render.runRender(param1);
         }
         for each(_loc11_ in this.wings)
         {
            _loc11_.part.render.runRender(param1);
         }
         for each(_loc11_ in this.others)
         {
            _loc11_.part.render.runRender(param1);
         }
         if(!InitData.CORE.pause && !InitData.CORE.over)
         {
            for each(_loc11_ in this.others)
            {
               if(_loc11_.part.render.inRender && Boolean(_loc11_.part.particlePort))
               {
                  _loc10_ = _loc11_.part.partMove.radFire + _loc11_.part.particlePort.disRad.y;
                  _loc10_ = Math.round(_loc10_ / param2) * param2;
                  _loc8_ = _loc11_.part.partMove.point.x + _loc11_.part.particlePort.disRad.x * Math.cos(_loc10_);
                  _loc9_ = _loc11_.part.partMove.point.y + _loc11_.part.particlePort.disRad.x * Math.sin(_loc10_);
                  if(param6 == 1)
                  {
                     _loc13_ = param3 * 2.5;
                     if(param3 > 0 && _loc13_ == 0)
                     {
                        _loc13_ = 1;
                     }
                     _loc12_ = -1;
                     while(++_loc12_ < _loc13_)
                     {
                        _loc7_ = new Particle(_loc11_.part.render.cam,_loc8_ - (_loc11_.part.render.cam.rect.x - _loc11_.part.render.cam.last.x),_loc9_ - (_loc11_.part.render.cam.rect.y - _loc11_.part.render.cam.last.y),Math.PI + _loc11_.part.partMove.radFire + Math.PI * (Math.random() * 30 - 15) / 180,Math.random() * 2,Math.random() * 2,30,0.95,1);
                        _loc7_.layer = 1;
                        _loc7_.flash = false;
                        _loc7_.spd.x += param4.x;
                        _loc7_.spd.y += param4.y;
                        param5.pa1.addOne1(_loc7_);
                     }
                  }
                  else
                  {
                     _loc13_ = param3 * 2.5 * param6;
                     if(param3 > 0 && _loc13_ == 0)
                     {
                        _loc13_ = 1;
                     }
                     _loc12_ = -1;
                     while(++_loc12_ < _loc13_)
                     {
                        _loc7_ = new Particle(_loc11_.part.render.cam,_loc8_ - (_loc11_.part.render.cam.rect.x - _loc11_.part.render.cam.last.x),_loc9_ - (_loc11_.part.render.cam.rect.y - _loc11_.part.render.cam.last.y),Math.PI + _loc11_.part.partMove.radFire + Math.PI * (Math.random() * 50 - 25) / 180,Math.random() * 7,Math.random() * 2,80,0.95,1);
                        _loc7_.layer = 1;
                        _loc7_.flash = false;
                        _loc7_.spd.x += param4.x;
                        _loc7_.spd.y += param4.y;
                        param5.pa1.addOne1(_loc7_);
                     }
                  }
               }
            }
         }
      }
   }
}

