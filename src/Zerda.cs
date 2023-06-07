using System.Diagnostics;
using System.Data;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System;
using System.Numerics;
using static SDL2.SDL;

public class Zerda
{
    struct Sector{
        public int id;
        public float floor, ceiling;
        public Wall[] walls;

        public Sector(int id, float floor, float ceiling, Wall[] walls){
            this.id = id;
            this.floor = floor;
            this.ceiling = ceiling;
            this.walls = walls;
        }
    }
    struct Wall{
        public Vector2 a, b;
        public int portal; //-1 = solid wall

        public Wall(float ax, float ay, float bx, float by, int portal){
            this.a = new Vector2(ax, ay);
            this.b = new Vector2(bx, by);
            this.portal = portal;
        }
    }
    struct Player{
        public Vector2 position;
        public Vector2 direction;

        public int currentSector = 0;

        public Player(Vector2 position, Vector2 direction){
            this.position = position;
            this.direction = direction;
        }
        public void move(uint deltaTime)
        {
            float speed = 0.01f;
            float rotspeed = 0.0025f; 
            
            if (IsKeyPressed(SDL_Keycode.SDLK_w))
            {
                position += direction * speed * deltaTime;
 
            }
            if (IsKeyPressed(SDL_Keycode.SDLK_s))
            {
                position -= direction * speed * deltaTime;
            }
            if (IsKeyPressed(SDL_Keycode.SDLK_a))
            {
                position -= new Vector2(direction.Y, -direction.X) * speed * deltaTime;
            }
            if (IsKeyPressed(SDL_Keycode.SDLK_d))
            {
                position += new Vector2(direction.Y, -direction.X) * speed * deltaTime;
            }

            if (IsKeyPressed(SDL_Keycode.SDLK_LEFT))
            {
                float angle = +rotspeed * deltaTime;
                direction = RotateVector(direction, angle);
            }
            if (IsKeyPressed(SDL_Keycode.SDLK_RIGHT))
            {
                float angle = -rotspeed * deltaTime;
                direction = RotateVector(direction, angle);
            }
        }

        Vector2 RotateVector(Vector2 vector, float angle)
        {
            float cosAngle = (float)Math.Cos(angle);
            float sinAngle = (float)Math.Sin(angle);
            return new Vector2(vector.X * cosAngle - vector.Y * sinAngle, vector.X * sinAngle + vector.Y * cosAngle);
        }
    }

    public const int wWidth = 800, wheight = 600;

    public nint w, r;
    public bool isRunning;

    public const float
    eyeheight = 1.5f,
    hfov = (float)(75 * Math.PI / 180.0),
    vfov = .5f;
    nint texture;
    uint[] pixels = new uint[wWidth * wheight];
    Sector[] map;

    void initMap(){
        Sector sector0 = new Sector(0, 0.0f, 4.0f, new Wall[] {
            new Wall(10.0f, 10.0f, 10.0f, 15.0f, -1),
            new Wall(10.0f, 15.0f, 12.5f, 17.5f, -1),
            new Wall(12.5f, 17.5f, 17.5f, 17.5f, -1),
            new Wall(17.5f, 17.5f, 20.0f, 15.0f, -1),
            new Wall(20.0f, 15.0f, 20.0f, 10.0f, 1),
            new Wall(20.0f, 10.0f, 17.5f, 7.5f, -1),
            new Wall(17.5f, 7.5f, 12.5f, 7.5f, -1),
            new Wall(12.5f, 7.5f, 10.0f, 10.0f, -1)
        });
        
        Sector sector1 = new Sector(1, 0f, 4.0f, new Wall[] {
            //new Wall(10.0f, 10.0f, 10.0f, 15.0f, 0),
            new Wall(25, 15, 25, 10, -1),
            new Wall(25, 10, 20, 10, -1),
            new Wall(20, 15, 25, 15, -1),
        
        });
        map = new Sector[] {sector0, sector1};
    }

   
    void UpdateTexture(nint texture)
    {
        IntPtr pixelsPtr;
        int pitch;
        SDL_LockTexture(texture, IntPtr.Zero, out pixelsPtr, out pitch);

        unsafe
        {
            uint* texturePixels = (uint*)pixelsPtr;
            for (int i = 0; i < pixels.Length; i++)
            {
                texturePixels[i] = pixels[i];
                pixels[i] = 0xb8b8ffff;
            }
            
        }

        SDL_UnlockTexture(texture);
    }
    



    public Zerda(){
        initMap();
        SDL_Init(SDL_INIT_VIDEO);
        w = SDL_CreateWindow("Zerda Engine - portal rendering", SDL_WINDOWPOS_UNDEFINED,SDL_WINDOWPOS_UNDEFINED, wWidth, wheight, SDL_WindowFlags.SDL_WINDOW_SHOWN);
        r = SDL_CreateRenderer(w, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
        texture = SDL_CreateTexture(r, SDL_PIXELFORMAT_RGBA8888, 1, wWidth, wheight);
        
        isRunning = true;
        
        uint time = 0, oldtime;
        while(isRunning){
            HandleEvents();

            oldtime = time;
            time = SDL_GetTicks();

            Loop(time - oldtime);

            UpdateTexture(texture);
            SDL_RenderCopy(r, texture, IntPtr.Zero, IntPtr.Zero);
            Render();
            SDL_RenderPresent(r);

        }
        SDL_DestroyTexture(texture);
        SDL_DestroyRenderer(r);
        SDL_DestroyWindow(w);
        SDL_Quit();

    }
    SDL_Event e;

    private static HashSet<int> keysPressed = new HashSet<int>();
    void HandleEvents(){
        while(SDL_PollEvent(out e) != 0){
            switch(e.type){
                case SDL_EventType.SDL_QUIT:
                    isRunning = false;
                break;
                case SDL_EventType.SDL_KEYDOWN:
                    keysPressed.Add((int)e.key.keysym.sym);
                    break;

                case SDL_EventType.SDL_KEYUP:
                    keysPressed.Remove((int)e.key.keysym.sym);
                    break;
            }
        }
    }
    public static bool IsKeyPressed(SDL_Keycode key)
    {
        return keysPressed.Contains((int)key);
    }

    Player p = new Player(new Vector2(15,15), new Vector2(0, -1));
    void Loop(uint dt){
       p.move(dt);
    }
    

    const int QUEUE_MAX = 64;
    private const float ZNEAR = 0.00001f;  
    private const float ZFAR = 100.0f; 

    int[] Ytop = new int[wWidth], Ybot = new int[wWidth];

      struct QueueEntry
        {
            public int id, x0, x1;
        };

        struct Queue
        {
            public QueueEntry[] arr;
            public int n;
        };
    void Render(){
        for (int i = 0; i < wWidth; i++) {
            Ytop[i] = wheight - 1;
            Ybot[i] = 0;
        }

        Vector2
         zdl = Rotate(new Vector2(0, 1), +(hfov / 2)),
         zdr = Rotate(new Vector2(0, 1), -(hfov / 2)),
         znl = new Vector2(zdl.X * ZNEAR, zdl.Y * ZNEAR),
         znr = new Vector2(zdr.X * ZNEAR, zdr.Y * ZNEAR),
         zfl = new Vector2(zdl.X * ZFAR, zdl.Y * ZFAR),
         zfr = new Vector2(zdr.X * ZFAR, zdr.Y * ZFAR);

         SDL_SetRenderDrawColor(r, 255, 255, 255, 255);
         SDL_RenderDrawLine(r, (int)p.position.X, (int)p.position.Y, (int)(p.position.X + p.direction.X * 4), (int)(p.position.Y + p.direction.Y * 4));

        
        //portals to draw queue
        bool[] sectdraw = new bool[128];
        Array.Fill(sectdraw, false);

        Queue queue = new Queue
        {
            arr = new QueueEntry[QUEUE_MAX],
            n = 1
        };
        
        queue.arr[0] = new QueueEntry { id = p.currentSector, x0 = 0, x1 = wWidth - 1 };
        

        while (queue.n != 0)
        {
            
            QueueEntry entry = queue.arr[--queue.n];

            if (sectdraw[entry.id])
            {
                continue;
            }
            sectdraw[entry.id] = true;
            
        

        foreach (Wall w in map[entry.id].walls){

        SDL_SetRenderDrawColor(r, 255, 0, 255, 255);
        if(w.portal != -1) SDL_SetRenderDrawColor(r, 0, 255, 255, 255);
        SDL_RenderDrawLine(r, (int)w.a.X, (int)w.a.Y, (int)w.b.X, (int)w.b.Y);
       
        Vector2
         op0 = worldtocam(w.a),
         op1 = worldtocam(w.b);

        Vector2 cp0 = op0, cp1 = op1;

        if(cp0.Y <= 0 && cp1.Y <= 0){
            continue;
        }

        float
         ap0 = NormalizeAngle((float)(Math.Atan2(cp0.Y, cp0.X) - Math.PI/2)),
         ap1 = NormalizeAngle((float)(Math.Atan2(cp1.Y, cp1.X) - Math.PI/2));

        if(cp0.Y < ZNEAR || cp1.Y < ZNEAR || ap0 > +(hfov/2) || ap1 < -(hfov/2)){
             
            Vector2
             il = IntersectSegs(cp0, cp1, znl, zfl),
             ir = IntersectSegs(cp0, cp1, znr, zfr);

            if(!float.IsNaN(il.X)){
            cp0 = il;
            ap0 = NormalizeAngle((float)(Math.Atan2(cp0.Y, cp0.X) - Math.PI/2));
            }
            if(!float.IsNaN(ir.X)){
            cp1 = ir;
            ap1 = NormalizeAngle((float)(Math.Atan2(cp1.Y, cp1.X) - Math.PI/2));
            }
        }
        if(ap0 < ap1){
        continue;
        }
        if((ap0 < -(hfov/2) && ap1 < -(hfov/2)) || (ap0 > +(hfov/2) && ap1 > +(hfov/2))){
        continue;
        }

        

        int
            tx0 = ScreenAngleToX(ap0),
            tx1 = ScreenAngleToX(ap1);

            if (tx0 > entry.x1) { continue; }
            if (tx1 < entry.x0) { continue; }

            int wallshade = (int)(16 * (Math.Sin(Math.Atan2(w.b.X - w.a.X, w.b.Y - w.a.Y)) + 1.0));

        int
            x0 = Math.Clamp(tx0, entry.x0, entry.x1),
            x1 = Math.Clamp(tx1, entry.x0, entry.x1);

        float
            zfloor = map[entry.id].floor,
            zceil = map[entry.id].ceiling,
            nsFloor = (w.portal >= 0) ? map[w.portal].floor : 0,  //alturas do proximo setor
            nsCeil = (w.portal >= 0) ? map[w.portal].ceiling : 0;

        float
            sy0 = IfNaN((vfov * wheight) / cp0.Y, (float)1e10),
            sy1 = IfNaN((vfov * wheight) / cp1.Y, (float)1e10);
            
            
        int
            yf0  = (wheight / 2) + (int) (( zfloor - eyeheight) * sy0),
            yc0  = (wheight / 2) + (int) (( zceil  - eyeheight) * sy0),
            yf1  = (wheight / 2) + (int) (( zfloor - eyeheight) * sy1),
            yc1  = (wheight / 2) + (int) (( zceil  - eyeheight) * sy1),
            nyf0 = (wheight / 2) + (int) ((nsFloor - eyeheight) * sy0),
            nyc0 = (wheight / 2) + (int) ((nsCeil  - eyeheight) * sy0),
            nyf1 = (wheight / 2) + (int) ((nsFloor - eyeheight) * sy1),
            nyc1 = (wheight / 2) + (int) ((nsCeil  - eyeheight) * sy1),
            txd = tx1 - tx0,
            yfd = yf1 - yf0,
            ycd = yc1 - yc0,
            nyfd = nyf1 - nyf0,
            nycd = nyc1 - nyc0;

        for(int x = x0; x <= x1; x++){
            int shade = x == x0 || x == x1 ? 192 : (255 - wallshade);
            
            float xp = IfNaN((x - tx0)/(float)txd, 0);

            int
                tyf = (int) (xp * yfd) + yf0,
                tyc = (int) (xp * ycd) + yc0,
                yf = Math.Clamp(tyf, Ybot[x], Ytop[x]),
                yc = Math.Clamp(tyc, Ybot[x], Ytop[x]);

            if(yf > Ybot[x]){
                Verline(
                        x,
                        Ybot[x],
                        yf,
                        0x222222ff);
            }
            if(yc < Ytop[x]){
                 Verline(
                        x,
                        yc,
                        Ytop[x],
                        0x000000FF);
            }

            if(w.portal != -1){
                int // calcula altura da "janela" do portal
                 tnyf = (int)(xp * nyfd) + yf0,
                 tnyc = (int)(xp * nycd) + yc0,
                 nyf = Math.Clamp(tnyf, Ybot[x], Ytop[x]),
                 nyc = Math.Clamp(tnyc, Ybot[x], Ytop[x]);

                  Verline(
                        x,
                        nyc,
                        yc,
                        0xFF00FF00);

                Verline(
                        x,
                        yf,
                        nyf,
                        0xFF0000FF);

                Ytop[x] = Math.Clamp(
                Math.Min(Math.Min(yc, nyc), Ytop[x]),
                0,
                wheight - 1);

                Ybot[x] = Math.Clamp(
                Math.Max(Math.Max(yf, nyf), Ybot[x]),
                0,
                wheight - 1);
            }else{
                Verline(x, yf, yc, RGBAShade(0x4488ffff, shade));
            }
        }

            if (w.portal != -1)
            {
                queue.arr[queue.n++] = new QueueEntry
                {
                    id = w.portal,
                    x0 = x0,
                    x1 = x1
                };

            }
        
        }
        }
        

    }
    uint RGBAShade(uint col, int a)
    {
        uint br = (uint)(((col & 0xFF00FF00) * a) >> 8);
        uint g = (uint)(((col & 0x00FF0000) * a) >> 8);

        return 0x000000FF | (br & 0xFF00FF00) | (g & 0x00FF0000);
    }
    float IfNaN(float x, float alt)
    {
        return float.IsNaN(x) ? alt : x;
    }
    
     void Verline(int x, int y0, int y1, uint color)
    {
        for (int y = y0; y <= y1; y++) {
            pixels[y * wWidth + x] = color;
        }
    }
    Vector2 worldtocam(Vector2 pos){
        Vector2 u = new Vector2(pos.X - p.position.X, pos.Y - p.position.Y);
        float x = u.X * p.direction.Y - u.Y * p.direction.X;
        float y = u.X * p.direction.X + u.Y * p.direction.Y;
        return new Vector2(x, y);
    }
    float NormalizeAngle(float a)
    {
        
        return a - (float)(Math.Tau * (float)Math.Floor((a + Math.PI) / Math.Tau));
    }

    Vector2 IntersectSegs(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
    {
        float d = ((a0.X - a1.X) * (b0.Y - b1.Y)) - ((a0.Y - a1.Y) * (b0.X - b1.X));

        if (MathF.Abs(d) < 0.000001f)
            return new Vector2(float.NaN, float.NaN);

        float t = (((a0.X - b0.X) * (b0.Y - b1.Y))
                - ((a0.Y - b0.Y) * (b0.X - b1.X))) / d;
        float u = (((a0.X - b0.X) * (a0.Y - a1.Y))
                - ((a0.Y - b0.Y) * (a0.X - a1.X))) / d;
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            float X = a0.X + (t * (a1.X - a0.X));
            float Y = a0.Y + (t * (a1.Y - a0.Y));
            return new Vector2(X, Y);
        }
        else
        {
            return new Vector2(float.NaN, float.NaN);
        }
    }
    public static int ScreenAngleToX(float a)
    {
        return
        (int)(((int) (wWidth / 2))
            * (1.0f - Math.Tan(((a + (hfov / 2.0)) / hfov) * Math.PI/2 - Math.PI/4)));
    }
    Vector2 Rotate(Vector2 v, float a){
        return new Vector2(
            (float)((v.X * Math.Cos(a)) - (v.Y * Math.Sin(a))),
            (float)((v.X * Math.Sin(a)) + (v.Y * Math.Cos(a)))
        );
    }
    static void Main(){
        new Zerda();
    }
}

