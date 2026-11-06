package f4.make
{
   import f4.data.InitData;
   import f4.data.sound32.SoundRun1;
   import f4.gfx.bmpdmc.BMPD2D;
   import f4.gfx.bmpdmc.BMPDMCData2;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.fire.FireLoop;
   import f4.unit.fire.FireMode;
   import f4.unit.fire.FirePort;
   import f4.unit.fire.FirePorts;
   import f4.unit.fire.FireSingle;
   import f4.unit.fire.FireTimer;
   import f4.unit.move.MoveAs;
   import f4.unit.particle.Item;
   import f4.unit.parts.FlyPart;
   import f4.unit.parts.SimpleFly;
   import f4.unit.parts.Unity;
   import f4.unit.render.Cam;
   import f4.unit.render.Render;
   import f4.unit.render.TRender;
   import f4.unit.upgrade.FlyData;
   import f4data.F4DataBullet;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.F4DataOrg;
   import flash.geom.Point;
   
   public class Maker
   {
      
      public var cam:Cam;
      
      public function Maker()
      {
         super();
      }
      
      public function makeAIFlyByData(param1:Number, param2:Number, param3:F4DataLinks, param4:Boolean, param5:Boolean = false, param6:Boolean = false) : AIUnity
      {
         var _loc7_:AIPart = null;
         var _loc8_:Unity = new Unity(param3.name,new MoveAs(param1,param2));
         _loc7_ = this.makeAIPartByData(param3.f4dataorg,param3.colorHue);
         _loc7_.part.main = true;
         _loc7_.org = param3.f4dataorg;
         _loc7_.org.dead = false;
         _loc8_.parts.push(_loc7_);
         this.makePartByLink(param3,_loc7_,_loc8_,param3.colorHue);
         _loc8_.udata = this.makeData();
         _loc8_.linkFriend(param4,param5 || param6);
         _loc8_.linkPartFirst();
         _loc8_.index = param3.index;
         var _loc9_:AIUnity = new AIUnity(_loc8_);
         _loc9_.aif.updateData(_loc8_.udata);
         _loc9_.unity.linkFireMode(_loc9_);
         _loc9_.initPartsRender();
         _loc9_.soundEngine = new SoundRun1(_loc9_.unity.move.point);
         InitData.SOUNDC32.soundRun.addOne1(_loc9_.soundEngine);
         _loc9_.aiTimer = InitData.TARGETS.levelTimer++;
         return _loc9_;
      }
      
      public function makePartByLink(param1:F4DataLinks, param2:AIPart, param3:Unity, param4:int) : void
      {
         var _loc5_:AIPart = null;
         var _loc6_:F4DataHole = null;
         for each(_loc6_ in param1.f4dataorg.hole)
         {
            if(_loc6_.partf4)
            {
               _loc5_ = this.makeAIPartByData(_loc6_.partf4.f4dataorg,param4);
               _loc5_.org = _loc6_.partf4.f4dataorg;
               _loc5_.org.dead = false;
               _loc5_.part.partMove.point.x = -_loc6_.xy.y + param2.part.partMove.point.x + _loc6_.partf4.f4dataorg.linkOff.y;
               _loc5_.part.partMove.point.y = _loc6_.xy.x + param2.part.partMove.point.y - _loc6_.partf4.f4dataorg.linkOff.x;
               _loc5_.part.initPartPoint(_loc5_.part.partMove.point.x,_loc5_.part.partMove.point.y);
               _loc5_.part.partMove.radFace = _loc6_.partf4.f4dataorg.rot / 180 * Math.PI;
               param3.parts.push(_loc5_);
               param2.onLoad.push(_loc5_);
               this.makePartByLink(_loc6_.partf4,_loc5_,param3,param4);
            }
         }
      }
      
      public function makeAIPartByData(param1:F4DataOrg, param2:int) : AIPart
      {
         var _loc7_:FirePort = null;
         var _loc8_:FireMode = null;
         var _loc11_:Number = NaN;
         var _loc12_:Number = NaN;
         var _loc3_:FlyPart = new FlyPart();
         var _loc4_:AIPart = new AIPart(_loc3_);
         _loc4_.hue = param2;
         var _loc5_:Render = this.makeRenderSec2(param1.mcName + param1.lr + "_" + param1.rot + "_" + param2,_loc3_.partMove);
         var _loc6_:FirePorts = new FirePorts();
         var _loc9_:int = -1;
         var _loc10_:int = param1.firePoints.length / 2;
         while(++_loc9_ < _loc10_)
         {
            _loc11_ = Number(param1.firePoints[_loc9_ * 2]);
            _loc12_ = Number(param1.firePoints[_loc9_ * 2 + 1]);
            _loc7_ = new FirePort(-_loc12_,_loc11_);
            _loc8_ = new FireMode();
            _loc8_.bulletData = param1.bullet;
            _loc8_.maker = this;
            _loc8_.fireAI = param1.bullet.aim + "d";
            _loc8_.firePorts = "ports";
            _loc8_.fireRender = param1.bullet.mcName;
            _loc8_.port = _loc7_;
            _loc8_.part = _loc4_;
            _loc8_.fireSg = new FireSingle(param1.fire.fireNum,param1.fire.angS,param1.fire.angAdd,param1.fire.spdS,param1.fire.spdAdd,param1.fire.accS,param1.fire.accAdd,param1.fire.accAngS,param1.fire.accAngAdd);
            _loc8_.fireLp = new FireLoop(0,0,0,0,0,0,0,0,0,false);
            _loc8_.fireTimer = new FireTimer(param1.fire.fireFrame,param1.fire.fireStop,param1.fire.fireEx,_loc9_ % 2 * param1.fire.fireFrame / 2);
            _loc7_.fireModes.push(_loc8_);
            _loc6_.ports.push(_loc7_);
         }
         _loc5_.cam = this.cam;
         _loc3_.render = _loc5_;
         _loc3_.firePorts = _loc6_;
         if(!isNaN(param1.partPoint.x))
         {
            _loc4_.part.particlePort = new FirePort(-param1.partPoint.y,param1.partPoint.x);
         }
         _loc3_.data = new FlyData(param1.name,new Point(param1.maxhp,param1.maxhp),new Point(0,param1.powFly),new Point(param1.turn,param1.turnFly),param1.fire.fireAcc,param1.mass);
         _loc3_.data.type = param1.type;
         _loc3_.hitRectOrg.x = param1.hit.x;
         _loc3_.hitRectOrg.y = param1.hit.y;
         _loc3_.partMove.makeHitRectFromWH(param1.hit.x,param1.hit.y);
         _loc3_.make4();
         _loc3_.index = param1.index;
         _loc4_.aif.updateData(_loc3_.data);
         return _loc4_;
      }
      
      public function makePorts(param1:String, param2:MoveAs) : FirePorts
      {
         var _loc3_:FirePorts = new FirePorts();
         if(param1 == "lr")
         {
            _loc3_.ports.push(this.makePort(3,0,param2,"dir"));
         }
         else if(param1 == "ud")
         {
            _loc3_.ports.push(this.makePort(3,0,param2,"muti"));
         }
         return _loc3_;
      }
      
      public function makePort(param1:Number, param2:Number, param3:MoveAs, param4:String) : FirePort
      {
         var _loc5_:FirePort = new FirePort(param1,param2);
         if(param4 == "dir")
         {
            _loc5_.fireModes.push(this.makeMode("normal",_loc5_,param3));
         }
         else if(param4 == "muti")
         {
            _loc5_.fireModes.push(this.makeMode("a2",_loc5_,param3));
         }
         return _loc5_;
      }
      
      public function makeMode(param1:String, param2:FirePort, param3:MoveAs) : FireMode
      {
         var _loc4_:FireMode = new FireMode();
         _loc4_.maker = this;
         _loc4_.fireAI = "ai";
         _loc4_.firePorts = "ports";
         _loc4_.fireRender = "render";
         _loc4_.port = param2;
         if(param1 == "a2")
         {
            _loc4_.fireSg = new FireSingle(1,0,0,200,0,0.1,0,0,0);
            _loc4_.fireLp = new FireLoop(0,0,0,0,0,0,0,0,0,false);
            _loc4_.fireTimer = new FireTimer(5,20,1);
         }
         return _loc4_;
      }
      
      public function makeRenderSec2(param1:String, param2:MoveAs) : Render
      {
         var _loc3_:Render = null;
         var _loc4_:BMPD2D = null;
         if(param1 == "normal")
         {
            _loc3_ = new Render(param2);
         }
         else
         {
            _loc4_ = (InitData.GDATA.mcList[param1] as BMPDMCData2).makeBMPD2D(false);
            _loc3_ = new TRender(param2);
            (_loc3_ as TRender).bmpd2d = _loc4_;
            (_loc3_ as TRender).initCkrect();
         }
         return _loc3_;
      }
      
      public function makeItem(param1:Number, param2:Number, param3:Array) : void
      {
         var _loc4_:int = 0;
         if(param3[0] == "fuel")
         {
            _loc4_ = int.MAX_VALUE;
         }
         else
         {
            _loc4_ = 450;
         }
         var _loc5_:Item = new Item(param3,(InitData.GDATA.gdata[param3[0]] as BMPDMCData2).makeBMPD2D(false),this.cam,param1,param2,Math.random() * Math.PI * 2,Math.random() * 4,0.95,_loc4_);
         InitData.TARGETS.ia1.addOne1(_loc5_);
      }
      
      public function makeRender(param1:String, param2:MoveAs) : Render
      {
         var _loc3_:Render = null;
         var _loc4_:BMPD2D = null;
         if(param1 == "normal")
         {
            _loc3_ = new Render(param2);
         }
         else
         {
            _loc4_ = (InitData.GDATA.gdata[param1] as BMPDMCData2).makeBMPD2D(false);
            _loc3_ = new TRender(param2);
            (_loc3_ as TRender).bmpd2d = _loc4_;
            (_loc3_ as TRender).initCkrect();
         }
         return _loc3_;
      }
      
      public function makeFly(param1:Number, param2:Number, param3:AIUnity, param4:F4DataBullet, param5:int) : SimpleFly
      {
         var _loc7_:Render = null;
         var _loc6_:SimpleFly = new SimpleFly(new MoveAs(param1,param2),param4,param3);
         if(param4.mcName != "normal")
         {
            InitData.GDATA.f4Blt(param4.mcName,param5);
            _loc7_ = this.makeRenderSec2(param4.mcName + "_" + param5,_loc6_.move);
            (_loc7_ as TRender).ani = false;
         }
         else
         {
            _loc7_ = this.makeRenderSec2(param4.mcName,_loc6_.move);
            _loc7_.head = true;
         }
         _loc7_.cam = this.cam;
         _loc6_.render = _loc7_;
         if(param4.aim > 0)
         {
            if(param3.unity.minLifePart)
            {
               _loc6_.aim = param3.unity.minLifePart.part.partMove.point;
            }
         }
         return _loc6_;
      }
      
      public function makePart(param1:String, param2:String) : AIPart
      {
         var _loc3_:FlyPart = new FlyPart();
         var _loc4_:FirePorts = this.makePorts(param1,_loc3_.partMove);
         var _loc5_:Render = this.makeRender(param2,_loc3_.partMove);
         _loc5_.cam = this.cam;
         _loc3_.render = _loc5_;
         _loc3_.firePorts = _loc4_;
         return new AIPart(_loc3_);
      }
      
      public function makeData() : FlyData
      {
         return new FlyData("shit",new Point(100,100),new Point(100,100),new Point(100,100),10,1000);
      }
   }
}

