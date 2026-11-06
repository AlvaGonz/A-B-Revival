package f4.unit.render
{
   import f4.gfx.bmpdmc.BMPD2D;
   import f4.unit.move.MoveAs;
   import flash.geom.Point;
   
   public class TRender extends Render
   {
      
      public var bmpd2d:BMPD2D;
      
      public var red:int;
      
      public var toDie:Boolean;
      
      public var timer:int;
      
      public var ani:Boolean;
      
      public function TRender(param1:MoveAs)
      {
         super(param1);
         this.ani = false;
      }
      
      public function initCkrect() : void
      {
         ckRect.width = this.bmpd2d.bmpd.rectSmall.width;
         ckRect.height = this.bmpd2d.bmpd.rectSmall.height;
      }
      
      override public function updateRect() : void
      {
         ckRect.x = move.point.x - ckRect.width / 2;
         ckRect.y = move.point.y - ckRect.height / 2;
         --this.red;
      }
      
      override public function render(param1:Number) : void
      {
         if(this.ani)
         {
            this.bmpd2d.bmpd.nextFrame();
            this.bmpd2d.bmpd.moveRect();
         }
         else
         {
            this.bmpd2d.changeBmpdRad(move.radFace + param1);
         }
         this.bmpd2d.makeDrawPoint(move.point.x - cam.rect.x,move.point.y - cam.rect.y);
         ++this.timer;
         if(this.red > 0)
         {
            this.bmpd2d.render(1);
         }
         else if(this.toDie)
         {
            if(this.timer % 8 < 3)
            {
               this.bmpd2d.render(2);
            }
            else
            {
               this.bmpd2d.render(0);
            }
         }
         else
         {
            this.bmpd2d.render(0);
         }
      }
      
      public function renderHitLines(param1:Point, param2:Array, param3:Number) : void
      {
         bline.lineRect32(param2[0].x + param1.x - cam.rect.x,param2[1].x + param1.x - cam.rect.x,param2[0].y + param1.y - cam.rect.y,param2[1].y + param1.y - cam.rect.y,param3,renderData.BMPD,65280);
         bline.lineRect32(param2[1].x + param1.x - cam.rect.x,param2[2].x + param1.x - cam.rect.x,param2[1].y + param1.y - cam.rect.y,param2[2].y + param1.y - cam.rect.y,param3,renderData.BMPD,65280);
         bline.lineRect32(param2[2].x + param1.x - cam.rect.x,param2[3].x + param1.x - cam.rect.x,param2[2].y + param1.y - cam.rect.y,param2[3].y + param1.y - cam.rect.y,param3,renderData.BMPD,65280);
         bline.lineRect32(param2[3].x + param1.x - cam.rect.x,param2[0].x + param1.x - cam.rect.x,param2[3].y + param1.y - cam.rect.y,param2[0].y + param1.y - cam.rect.y,param3,renderData.BMPD,65280);
      }
   }
}

