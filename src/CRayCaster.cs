using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;


/*
 * Based on C program in Tricks of the Game Programming Gurus
 * by LaMothe, Ratcliff, Seminatore & Tyler, SAMS Publishing. 
*/
namespace raycaster
{

    /*
     * form coord system = +X to the right, +Y down
     * virtual coord system = +X to the right, +Y up     
    */
    class CRayCaster
    {
        /*
         * Constants used to index into a table with precalculated data for 
         * each ray sent out. The total number of rays is 1920 when using
         * a res of 320x240. 
         * 
         * This is because we divide 360 up by the FOV (60 degrees) which = 6. 
         * All 6 FOVs need precalculated rays for every pixel (all 320 of them) thus:
         * 320 * 360/60 = 1920
         * 
         * Converting:
         * angIndex -> angle = angIndex * 0.1875
         * 1920 * 0.1875 = 360
         * 80 * 0.1875 = 15
        */
        private const int ANGLE_0 = 0;
        private const int ANGLE_1 = 5;
        private const int ANGLE_2 = 10;
        private const int ANGLE_4 = 20;
        private const int ANGLE_5 = 25;
        private const int ANGLE_6 = 30;
        private const int ANGLE_15 = 80;
        private const int ANGLE_30 = 160;
        private const int ANGLE_45 = 240;
        private const int ANGLE_60 = 320;
        private const int ANGLE_90 = 480;
        private const int ANGLE_135 = 720;
        private const int ANGLE_180 = 960;
        private const int ANGLE_225 = 1200;
        private const int ANGLE_270 = 1440;
        private const int ANGLE_315 = 1680;
        private const int ANGLE_360 = 1920;

        private const int WORLD_ROWS = 16;
        private const int WORLD_COLUMNS = 16;

        private const int CELL_Y_SIZE = 64;
        private const int CELL_X_SIZE = 64;

        private const int INTERSECTION_FOUND = 1;


        private Color m_backColor;

        private Image m_img;
        public Image getImg() { return m_img; }
        private int m_width;
        private int m_height;


        private char[,] m_world = new char[,] {
            {'1','1','1','1','1','1','1','1','1','1','1','1','1','1','1','1'},
            {'1','0','0','0','0','0','0','0','0','0','0','0','0','0','0','1'},
            {'1','0','0','1','1','1','1','1','0','0','0','0','0','0','0','1'},
            {'1','0','0','1','0','0','0','1','0','1','0','1','0','1','0','1'},
            {'1','0','0','1','0','0','1','1','0','0','0','0','0','0','0','1'},
            {'1','0','0','1','0','0','0','1','0','0','0','0','0','0','0','1'},
            {'1','0','0','0','0','0','0','0','0','0','0','0','0','0','0','1'},
            {'1','0','0','0','0','0','0','0','0','0','0','0','0','0','0','1'},
            {'1','0','0','0','0','0','0','0','0','0','0','0','0','0','0','1'},
            {'1','0','0','1','1','0','0','1','1','1','1','1','1','0','0','1'},
            {'1','0','0','1','0','0','0','0','0','0','0','0','1','0','0','1'},
            {'1','0','0','1','1','1','0','0','0','0','0','0','1','0','0','1'},
            {'1','0','0','1','0','0','0','0','0','0','0','0','1','0','0','1'},
            {'1','0','0','1','1','1','1','1','1','1','1','0','1','0','0','1'},
            {'1','0','0','0','0','0','0','0','0','0','0','0','0','0','0','1'},
            {'1','1','1','1','1','1','1','1','1','1','1','1','1','1','1','1'},
        };        
        
        private int m_x = 8 * 64 + 25;
        private int m_y = 3 * 64 + 25;
        private int m_viewAngle = ANGLE_60;        
        

        //precalc tables
        private double[] m_tanTable = new double[ANGLE_360 + 1];
        private double[] m_recipTanTable = new double[ANGLE_360 + 1];

        private double[] m_yStep = new double[ANGLE_360 + 1];
        private double[] m_xStep = new double[ANGLE_360 + 1];
        
        private double[] m_recipCosTable = new double[ANGLE_360 + 1];
        private double[] m_recipSinTable = new double[ANGLE_360 + 1];
            



        public CRayCaster(Image img, Color backColor)
        {
            m_img = img;
            m_backColor = backColor;
            m_width = m_img.Width;
            m_height = m_img.Height;
        }

        private void drawLine(int x1, int y1, int x2, int y2, Color c)
        {
            using (Graphics g = Graphics.FromImage(m_img))
            {
                using (Pen pen = new Pen(c, 1))
                {
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        private double toRadians(double degreesVal)
        {
            return (Math.PI / 180) * degreesVal;
        }



        
        private void buildTables()
        {
            double radAngle = 0.0d;
            double slope = toRadians(360.0d / ANGLE_360);

            for (int angIndex = ANGLE_0; angIndex <= ANGLE_360; angIndex++)
            {
                /*
                 * Presuming we have the following triangle:
                 * 
                 *    hyp   *
                 *    4.9* 	*
			     *     *	* 2.8
		         *  *35)	* opp
		         *  *********
			     *      4.0
                 *      adj
                 * 
                 * tanTable[35 degrees] = 0.7 = ratio between opp and adj (2.8/4.0 = 0.7) 
                 * which means for every +1 to X +0.7 to Y
                 * 0.7 * 4 = 2.8
                 * 
                 * recipTanTable[35 degrees] = 1/0.7 = 1.4285714
                 * which means for every +1.4285714 to Y +1 to X
                 * 1.4285714.. * 2.8 = 4
                 *                  
                 *                 
                */
                radAngle = (slope/10) + (angIndex * slope);
                m_tanTable[angIndex] = Math.Tan(radAngle);
                m_recipTanTable[angIndex] = 1 / m_tanTable[angIndex];

                /*
                 * Below presumes:
                 * 
                 *        90
                 *        .+y
                 *        .
                 *    -x  .   +x
                 * 180...........0
                 *        .
                 *        .
                 *        .-y
                 *       270
                */


                /*
                 * To move y by 1 unit and maintain 35 degrees, you have to add 0.7. If you
                 * know you want to move 64 units then it's:
                 * 0.7 * 64 or tanTable[35 degrees] * CELL_Y_SIZE
                */
                //nextYi = yi + (angleSlope * CELL_Y_SIZE)
                if (angIndex >= ANGLE_0 && angIndex < ANGLE_180)
                {
                    m_yStep[angIndex] = Math.Abs(m_tanTable[angIndex] * CELL_Y_SIZE);
                }
                else
                {
                    m_yStep[angIndex] = -Math.Abs(m_tanTable[angIndex] * CELL_Y_SIZE);
                }

                
                if (angIndex >= ANGLE_90 && angIndex < ANGLE_270)
                {
                    m_xStep[angIndex] = -Math.Abs(m_recipTanTable[angIndex] * CELL_X_SIZE);
                }
                else
                {
                    m_xStep[angIndex] = Math.Abs(m_recipTanTable[angIndex] * CELL_X_SIZE);
                }
                
                m_recipCosTable[angIndex] = 1 / Math.Cos(radAngle);
                m_recipSinTable[angIndex] = 1 / Math.Sin(radAngle);
            }                   
        }


        /*                  
         * Presumptions:
         * ) Field of view (FOV) = 60 degrees
         * ) Screen resolution = 320 x 240
         * ) World consists of 16x16 cells where each cell is 64x64 virtual units
         * ) Map must be surrounded by a wall
         * 
         * Overview:
         * ) We must cast 320 rays of light from the player which will cover 60 degrees
         *   starting from view angle - 30 and going through to view angle + 30 in 60/320 
         *   increments.
         * ) To calculate what the world look likes based on the angle for each ray do the
         *   following:
         *      ) if the view angle is 0 - 179 degrees i.e. looking up 
         *       (process is very similar for looking down):
         *                              
         *        |‾ ‾ ‾ ‾ ‾↗‾ ‾| ← get the y coord for this top line then get
         *        |       ↗     |   the x coord (xi) for where the arrow intersects
         *        |     P        |   it
         *        |              |
         *        
         *      ) if the view angle is < 90 or >= 270 i.e. looking right (similar process
         *        for looking left) :
         *              90      ↑
         *              .       | 
         *              .       |
         *      180..... .....0 | i.e. Somewhere between these two on the right side
         *              .       |
         *              .       |
         *             270      ↓
         *        
         *        then:         get the x coord for this line then get the y coord (yi)
         *                      for where the arrow intersects it
         *                      ↓
         *        |‾ ‾ ‾ ‾ ‾P‾ ‾|
         *        |          ↘ |
         *        |            ↘
         *        |             |         
         *        
         *      ) keep increasing the ray length at the same angle until we hit a horizontal or
         *        vertical line that is a solid wall         
         *      ) once a wall has been hit, calculate the distance to it i.e. find the length
         *        of the hypotenuse
         *      ) Once we've hit both a vertical and horizontal wall, find out which is the
         *        closest (Note: when making the level, ensure that a wall surrounds it
         *        otherwise we could get into an infinite loop here)
         *      ) Convert shortest distance from polar to cartesian coords, invert it  
         *        (i.e. 1 / distance) so that things are smaller the further away they 
         *        are then scale it so it looks good when drawn         
         *      ) Finally, draw a vertical line to the screen starting center right
         *      ) Optionally also draw lines for the sky above it and the floor below it         
        */
        private void castRay(int x, int y, int viewAngle)
        {
            int yBound, xBound;
            int yDelta, xDelta;
            int nextYCell, nextXCell;
            int cellXi, cellYi;            
            double distX=0, distY=0;
            double yiSave=0, xiSave=0;
            double scale;
            int top, bottom;
            

            double xi, yi; //x intersection, y intersection


            //view angle is the angle at which you're looking at the scene so subtract half
            //the FOV (FOV=60) so that the rays can sweep across covering everything between
            //viewangle - 30 and viewangle + 30
            viewAngle -= ANGLE_30;
            if (viewAngle < 0)
            {
                viewAngle = ANGLE_360 + viewAngle;
            }

            for (int rayIndex = 0; rayIndex < 320; rayIndex++)
            {                
                if (viewAngle >= ANGLE_0 && viewAngle < ANGLE_180)
                {
                    /*
                     * Reasoning behind: CELL_Y_SIZE + (CELL_Y_SIZE * (m_y / CELL_Y_SIZE))
                     * Take an example where m_y = 240 & CELL_Y_SIZE = 64:
                     * 64 + (64 * (240/64)) = 256 (240/64 = 3 NOT 3.75)
                     * 
                     * So this formula works by:
                     * )works out which cell you are in using 240/64
                     * )converts cell number back to global position by multiplying by 64
                     * )adds 64 to get the horizontal line at the top of the cell that you're in
                     * 
                     * Diagram:
                     * 1) we get to this cell using 240/64
                     * |¯¯¯¯¯¯¯¯|← 3) adding 64 gets you here (presuming y increases as you move up)
                     * |        |    
                     * |        |
                     * |________|← 2) you're here when you do (240/64) * 64 (fine for looking down)
                     * 
                    */

                    //first horizontal line that could be intersected with ray above player                    
                    yBound = CELL_Y_SIZE + (CELL_Y_SIZE * (y / CELL_Y_SIZE));
                    yDelta = CELL_Y_SIZE;

                    //xi = M^-1 (yi - yp) + xp
                    xi = (m_recipTanTable[viewAngle] * (yBound - y)) + x;                    
                    nextYCell = 0;
                }
                else
                {
                    //first horizontal line that could be intersected with ray below player                    
                    yBound = CELL_Y_SIZE * (y / CELL_Y_SIZE);
                    yDelta = -CELL_Y_SIZE;

                    xi = (m_recipTanTable[viewAngle] * (yBound - y)) + x;
                    nextYCell = -1;
                }


                if (viewAngle < ANGLE_90 || viewAngle >= ANGLE_270)
                {
                    /*
                     * Diagram:
                     * 1) we get to this cell using 240/64
                     * 
                     * 2        3
                     * ↓        ↓
                     * |¯¯¯¯¯¯¯¯|
                     * |        |    
                     * |        |
                     * |________|
                     *                      
                     * 2) you're here when you do (240/64) * 64 (this is fine for when looking left)
                     * 3) adding 64 gets you here (presuming X increases as you move right)
                    */

                    //first vertical line that could be intersected with ray right of the player                    
                    xBound = CELL_X_SIZE + (CELL_X_SIZE * (x / CELL_X_SIZE));                    
                    xDelta = CELL_X_SIZE;
                    
                    yi = (m_tanTable[viewAngle] * (xBound - x)) + y;
                    nextXCell = 0;

                }
                else
                {
                    //first vertical line that could be intersected with ray left of the player                    
                    xBound = CELL_X_SIZE * (x / CELL_X_SIZE);                    
                    xDelta = -CELL_X_SIZE;

                    yi = (m_tanTable[viewAngle] * (xBound - x)) + y;
                    nextXCell = -1;
                }


                int casting = 2;
                int xray = 0;
                int yray = 0;
                while (casting > 0)
                {
                    if (xray != INTERSECTION_FOUND)
                    {
                        //looking for Y interesctions i.e. vertical boundaries                        


                        /* ↑ ↓ → ←  ↖↗↘↙
                         * Diagram
                         * P1 = player location
                         * P1:
                         * CellX = 8 
                         * (x, y) = (537, 240)
                         * xBound =  64 + (64 * (537 / 64)) = 576
                         * cellXi = (576 + 0) / 64 = 9
                         * 
                         * Presuming view angle = 45 degrees (indicated by P1 arrow):
                         * yi = m_tanTable[45 degrees] * ((xBound - m_x) + m_y);
                         * yi = 1 * (576 - 537) + 240 = 279
                         * cellYi = (279 / 64) = 4
                         * 
                         *                xBound, nextXCell = 0
                         *                   ↓
                         * |¯¯¯¯¯¯¯¯|¯¯¯¯¯¯¯¯|¯¯¯¯¯¯¯¯|
                         * |        |        |        |
                         * |        |        |        |CellY=4
                         * |        |        |        |
                         * |¯¯¯¯¯¯¯¯|¯¯¯↗¯¯¯|¯¯¯¯¯¯¯¯|
                         * |        | P1     |        |
                         * |        |        |        |CellY=3
                         * |________|________|________|
                         *  CellX=7   CellX=8  CellX=9              
                        */
                        cellXi = ((xBound + nextXCell) / CELL_X_SIZE);
                        cellYi = (int)(yi / CELL_Y_SIZE);
                                                
                        if (cellYi >= WORLD_ROWS)
                            { cellYi = WORLD_ROWS - 1; }
                        if (cellYi < 0)
                            { cellYi = 0; }
                        
                        //subtract from m_world because m_world = Y+ going down but our model
                        //is Y-, using the example above:
                        //(16 - 1) - 4 = 11
                        if ((m_world[(WORLD_ROWS - 1) - cellYi, cellXi]) == '1')
                        {
                            /* Presuming we have the following triangle:
                             * 
                            *       4.9        *
                             *       hyp    *   * 2.8
                             *           *      * opp
                             *        *         *
                             *     * 35°        *
                             *    ***************
                             *          4.0
                             *          adj
                             *      
                             * sin = opp/hyp = 2.8/4.9 = 0.5714...
                             * recipSin = 1/0.5714... = 1.75
                             * distX = 2.8 * 1.75 = 4.9
                            */
                            distX = (yi - y) * m_recipSinTable[viewAngle];                            
                            yiSave = yi;                            
                            
                            xray = INTERSECTION_FOUND;
                            casting--;
                        }                        
                        else
                        {
                            // compute next Y intercept
                            yi += m_yStep[viewAngle];
                        }
                    }




                    if (yray != INTERSECTION_FOUND)
                    {
                        //looking for X interesctions i.e. horizontal boundaries                       

                        cellXi = (int)(xi / CELL_X_SIZE);
                        cellYi = ((yBound + nextYCell) / CELL_Y_SIZE);
                        
                        if (cellXi >= WORLD_COLUMNS)
                            { cellXi = WORLD_COLUMNS - 1; }
                        if (cellXi < 0)
                            { cellXi = 0; }
                        
                        if ((m_world[(WORLD_ROWS - 1) - cellYi, cellXi]) == '1')
                        {
                            /* Presuming we have the following triangle:                           
                             *
                             *       4.9        *
                             *       hyp    *   * 2.8
                             *           *      * opp
                             *        *         *
                             *     * 35°        *
                             *    ***************
                             *          4.0
                             *          adj
                             *          
                             * cos = adj/hyp = 4.0/4.9 = 0.816326...
                             * recipCos = 1/0.816326... = 1.225
                             * distY = 4.0 * 1.225 = 4.9
                            */
                            distY = (xi - x) * m_recipCosTable[viewAngle];
                            xiSave = xi;                            
                            yray = INTERSECTION_FOUND;
                            casting--;

                        }
                        else
                        {                            
                            xi += m_xStep[viewAngle];
                        }
                    }

                    xBound += xDelta;
                    yBound += yDelta;
                }

                
                if (distX < distY)
                {
                    //there was a vertical wall closer than the horizontal                    

                    //Explained below for horizontal walls
                    int currAngIndex = Math.Abs(160 - rayIndex);
                    double radAngle = currAngIndex * ((2 * Math.PI) / ANGLE_360);                    
                    scale = 15000 * (1 / ( distX * Math.Cos(radAngle) ));
                                                            
                    //m_height = 200
                    //(200/2) - (0.20408 / 2) = 99.8979591
                    top = (int)( (m_height/2) - (scale / 2) );
                    if (top < 0)
                    {
                        top = 0;
                    }

                    bottom = (int)(top + scale);
                    if ( bottom >= m_height )
                    {
                        bottom = m_height - 1;
                    }

                    // draw wall sliver and place some dividers up
                    /*
                     * Diagram:
                     * 
                     * |¯¯¯¯¯¯¯¯|← yiSave = 64 % 64 = 0
                     * |        |    
                     * |        |
                     * |________|← yiSave = 1 & yiSave = 0:
                     *              1 % 64 = 1
                     *              0 % 64 = 0                                          
                    */
                    Color sliverColor = Color.Green;
                    if (((int)yiSave) % CELL_Y_SIZE <= 1)
                    {
                        sliverColor = Color.White;
                    }
                    else
                    {
                        sliverColor = Color.Green;
                    }                   

                    //draw vertical lines from right to left
                    int x1 = (m_width - 1) - rayIndex;
                    int y1 = top;
                    int x2 = (m_width - 1) - rayIndex;
                    int y2 = bottom;
                    drawLine(x1, y1, x2, y2, sliverColor);
                    drawLine(x1, 0, x2, top - 1, m_backColor);
                    drawLine(x1, bottom, x2, m_height, m_backColor);                    
                }
                else
                {
                    //hit a horizontal wall first                    

                    /* 
                     * First thing to do is convert from polar coords to cartesian (AKA rectangular)
                     * coords to remove the fish bowl effect for each relative angle spanning
                     * 30 degrees from the left to 30 degrees to the right which makes up the 60
                     * degrees FOV (30 .... 0 ..... 30):
                     * rayindex = 319, angle = 30......rayindex = 160, angle = 0......rayindex = 0, angle = 30
                     *                      
                     *           opp
                     *      |‾ ‾ ‾ ‾ ‾ /
                     *      |         /
                     *      |        /
                     *      |       /
                     *  ?   |      /
                     *  adj |     / distX
                     *      |    /  hyp
                     *      |---/
                     *      |θ /
                     *      | /
                     *      |/
                     *
                     * distX * cos(θ) = adj
                     * 
                     * So initially θ = 30 presuming view angle approx = 90 degrees i.e. looking
                     * straight up. 
                     * 
                     * Next we must invert adj so that things get smaller as they 
                     * get further away so divide 1 by adj. Finally, scale the value so it looks
                     * good by multiplying by a number, here we've used 15000 to get a squareish 
                     * look.
                    */
                    int currAngIndex = Math.Abs(160 - rayIndex);
                    double radAngle = currAngIndex * ((2 * Math.PI) / ANGLE_360);                    
                    scale = 15000 * (1 / ( distY * Math.Cos(radAngle) ));
                    
                    top = (int)((m_height / 2) - (scale / 2));
                    if (top < 0)
                    {
                        top = 0;
                    }

                    bottom = (int)(top + scale);
                    if (bottom >= m_height)
                    {
                        bottom = m_height - 1;
                    }

                    Color sliverColor = Color.Green;
                    if (((int)xiSave) % CELL_X_SIZE <= 1)
                    {
                        sliverColor = Color.White;
                    }
                    else
                    {
                        sliverColor = Color.DarkGreen;
                    }                    

                    //draw vertical lines from right to left
                    int x1 = (m_width - 1) - rayIndex;
                    int y1 = top;
                    int x2 = (m_width - 1) - rayIndex;
                    int y2 = bottom;
                    drawLine(x1, y1, x2, y2, sliverColor);
                    drawLine(x1, 0, x2, top - 1, m_backColor);
                    drawLine(x1, bottom, x2, m_height, m_backColor);                    
                }

                if (++viewAngle >= ANGLE_360)
                {                    
                    viewAngle = 0;
                }                
            }
        }

        public void init()
        {
            buildTables();            
           
            castRay(m_x, m_y, m_viewAngle);            
        }

        public void turnLeft()
        {
            m_viewAngle += ANGLE_6;
            if (m_viewAngle >= ANGLE_360)
                { m_viewAngle = ANGLE_0; }            
        }

        public void turnRight()
        {
            m_viewAngle -= ANGLE_6;
            if (m_viewAngle < ANGLE_0)
                { m_viewAngle = ANGLE_360; }            
        }


        private void move(bool forwards)
        {
            double viewAngleRads = (2 * Math.PI * m_viewAngle) / ANGLE_360;
            int amountToMove = 10;
            double dx = Math.Cos(viewAngleRads) * amountToMove;
            double dy = Math.Sin(viewAngleRads) * amountToMove;

            if (!forwards)
            {
                dx = -dx;
                dy = -dy;                
            }

            m_x += (int)dx;
            m_y += (int)dy;            

            //collision detection
            const int MIN_DISTANCE_FROM_WALL = 15;

            int xCell = m_x / CELL_X_SIZE;
            int yCell = m_y / CELL_Y_SIZE;
            
            int xSubCell = m_x % CELL_X_SIZE;
            int ySubCell = m_y % CELL_Y_SIZE;

            if (dx > 0)
            {
                //going right
                if ((m_world[(WORLD_ROWS - 1) - yCell, xCell + 1] == '1') &&
                    xSubCell > (CELL_X_SIZE - MIN_DISTANCE_FROM_WALL))                    
                {
                    m_x = (xCell * CELL_X_SIZE) + (CELL_X_SIZE - MIN_DISTANCE_FROM_WALL);                    
                }
            }
            else
            {
                //going left
                if ((m_world[(WORLD_ROWS - 1) - yCell, xCell - 1] == '1') &&
                   xSubCell < MIN_DISTANCE_FROM_WALL)
                {
                    m_x = (xCell * CELL_X_SIZE) + MIN_DISTANCE_FROM_WALL;                    
                }
            }

            if (dy > 0)
            {
                //going up
                if ((m_world[(WORLD_ROWS - 1) - (yCell + 1), xCell] == '1') &&
                    ySubCell > (CELL_Y_SIZE - MIN_DISTANCE_FROM_WALL))
                {
                    m_y = (yCell * CELL_Y_SIZE) + (CELL_Y_SIZE - MIN_DISTANCE_FROM_WALL);                    
                }
            }
            else
            {
                //going down
                if ((m_world[(WORLD_ROWS - 1) - (yCell - 1), xCell] == '1') &&
                    ySubCell < MIN_DISTANCE_FROM_WALL)
                {
                    m_y = (yCell * CELL_Y_SIZE) + MIN_DISTANCE_FROM_WALL;                    
                }
            }
        }

        public void moveForwards()
        {
            move(true);           
        }

        public void moveBackwards()
        {
            move(false);           
        }

        public void run()
        {
            castRay(m_x, m_y, m_viewAngle); ;            
        }
    }
}
