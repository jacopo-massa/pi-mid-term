#nowarn
#load "lwc.fsx"

open Lwc
open System
open System.Drawing
open System.Drawing.Drawing2D
open System.Windows.Forms
open System.Numerics

let defaultColor = Color.LightGray // colore con cui vengono disegnati i tracciati
let selectionColor = Color.LightBlue // colore applicati ai tracciati quando selezionati
let rand = new Random()
let dimBall = 15.f // dimensione della pallina disegnata durante le animazioni

// tipo che rappresenta un tracciato
type Route(initialPos:PointF) as this =
    inherit LWControl()
    
    let path = new GraphicsPath() //insieme di punti del tracciato
    let mutable pen = new Pen(Color.LightGray, 18.f)
    let brush = new SolidBrush(Color.FromArgb(rand.Next(256), rand.Next(255), rand.Next(256)))
    let mutable drag = None
    let mutable initialPoint = PointF(0.f, 0.f)
    let mutable idx = 0
    let timer = new Timer(Interval=rand.Next(50,120))
    
    do
        this.Position <- initialPos
        this.Size <- SizeF(0.f, 0.f)
        timer.Tick.Add(fun _ ->
            idx <- idx + 1
            this.Invalidate())
    done
    
    let distance (p1:PointF) (p2:PointF) =
        float32(Math.Sqrt(Math.Pow(float (p2.X - p1.X),float 2) + Math.Pow(float(p2.Y - p1.Y), float 2)))
    
    let computeSize (positionW:PointF) =
        let positionV = this.Matrixs.TransformPointV(positionW)
        
        if(positionW.X < this.Left) then
            this.Width <- this.Width + (this.Left - positionW.X)
            this.Left <- positionW.X
            initialPoint.X <- positionV.X
        else
            if(this.Left + this.Width < positionW.X) then
                this.Width <- positionW.X - this.Left

        if(positionW.Y < this.Top) then
            this.Height <- this.Height + (this.Top - positionW.Y)
            this.Top <- positionW.Y
            initialPoint.Y <- positionV.Y
        else
            if(this.Top + this.Height < positionW.Y) then
                this.Height <- positionW.Y - this.Top
     
    member this.Offset with get() = PointF(this.Left - initialPoint.X, this.Top - initialPoint.Y)
    member this.Path with get() = path
    member this.Pen with get() = pen   
    member this.Timer with get() = timer
    member this.AnimationIndex
        with get() = idx
        and set(i:int) = idx <- i
        
    member this.RouteContainer
        with get() =
            match this.Parent with
            | Some(p) -> (p :?> Autodrome)
            | None -> new Autodrome()
            
    member this.Points
        with get() = path
        

    member this.IsIn (p:PointF) =
        path.IsOutlineVisible(p,this.Pen)
        
    member this.AddPoint (p:PointF) =
        let vp = this.Matrixs.TransformPointV(p)
        if(path.PointCount = 0) then
            path.StartFigure()
            path.AddLine(PointF(0.f, 0.f), vp)
        else
            path.AddLine(path.GetLastPoint(), vp)
        computeSize p
            
    member this.EndRoute =
        if(path.PointCount <> 0) then
            let first = path.PathPoints.[0]
            let interval = 10.f
            let mutable last = path.GetLastPoint()
            let mutable dist = distance last first
            let vector = Vector2.Multiply(Vector2.Normalize(new Vector2(first.X - last.X, first.Y - last.Y)), interval)
            while (dist > interval) do
                let newP = PointF(last.X + vector.X, last.Y + vector.Y)
                path.AddLine(last, newP)
                last <- newP
                dist <- distance first last
            path.CloseFigure()
        
    override this.OnMouseDown e =
        match this.RouteContainer.Operation with
        | "SELECT" ->
            if not (this.RouteContainer.UpdateSelectedRoute(this)) then
                pen.Color <- selectionColor
            else
                pen.Color <- defaultColor
        | "MOVE" -> pen.Color <- selectionColor
        | _ -> ()
        
        let wMouse = this.Matrixs.TransformPointW(PointF(single e.X, single e.Y))
        drag <- Some(PointF(wMouse.X - this.Left, wMouse.Y - this.Top)) 
        this.Invalidate()
        
    (*override this.OnMouseMove e =
        let wMouse = this.Matrixs.TransformPointW(PointF(single e.X, single e.Y))
        match this.RouteContainer.Operation with
        | "MOVE" ->
            match drag with
            | Some(offset) ->
                let mutable newPos = PointF(wMouse.X - offset.X, wMouse.Y - offset.Y)
                if (newPos.X < 0.f) then newPos.X <- 0.f
                if (newPos.X + this.Width > single this.RouteContainer.Width) then newPos.X <- single this.RouteContainer.Width - this.Width
                if (newPos.Y < 0.f) then newPos.Y <- 0.f
                if (newPos.Y + this.Height > single this.RouteContainer.Height) then newPos.Y <- single this.RouteContainer.Height - this.Height
                this.Position <- newPos
                pen.Color <- selectionColor
            | None -> ()
        | _ -> ()*)
        
    override this.OnMouseUp e =
        drag <- None
        this.Invalidate()
            
                    
        
    override this.OnPaint(e) =
        e.Graphics.DrawPath(pen, path)
        
        if(timer.Enabled) then
            if(idx = path.PointCount) then idx <- 0
            let mutable p = path.PathPoints.[idx]
            e.Graphics.FillEllipse(brush,p.X - dimBall/2.f, p.Y - dimBall/2.f, dimBall, dimBall)

// tipo che rappresenta il container con i controlli grafici
and Autodrome() as this =
    inherit LWContainer()
    let selectedRoutes = ResizeArray<Route>()
    let mutable movingRoute = None
    let mutable operation = null
    let mutable tempRoute = None
    let mutable center = PointF()
    
    member this.Operation
        with get() = operation
        and set(o:string) = operation <- o
        
     member this.Center
        with get() = center
        and set(v: PointF) = center <- v
        
    member this.SelectedRoutes with get() = selectedRoutes
    
    // seleziona/deseleziona un tracciato
    member this.UpdateSelectedRoute(r:Route) =
        let result = selectedRoutes.Contains(r)
        match result with
        | true -> selectedRoutes.Remove(r) |> ignore
        | false ->
            selectedRoutes.Add(r)
        this.Invalidate()
        printfn "res %A len %A" result selectedRoutes.Count
        result
        
    member this.AnimateRoutes =
        selectedRoutes |> Seq.iter(fun sr -> sr.Timer.Start())
    
     member this.StopAnimateRoutes =
        selectedRoutes |> Seq.iter(fun sr ->
            sr.Timer.Stop()
            sr.AnimationIndex <- 0
            this.Invalidate())
        
    member this.DeleteRoutes =
        selectedRoutes |> Seq.iter(fun sr ->
            if(sr.Timer.Enabled) then
                sr.Timer.Stop()
                sr.AnimationIndex <- 0
            this.LWControls.Remove(sr) |> ignore)
        selectedRoutes.Clear()
        this.Invalidate()
    
    //funzioni per effettuare trasformazioni  dei singoli tracciati o del container
    member this.Translate(tx, ty, isWorld) =
        if(isWorld) then //traslo il mondo
            this.LWControls |> Seq.iter(fun c -> c.Position <- PointF(c.Position.X + tx, c.Position.Y + ty))
        else //traslo le viste selezionate
            selectedRoutes |> Seq.iter(fun sr -> sr.Position <- PointF(sr.Position.X + tx, sr.Position.Y + ty))
        this.Invalidate()
        
    member this.Zoom(scaleFactor, isWorld) =
        if(isWorld) then //scalo il mondo
            this.LWControls |> Seq.iter(fun c ->
                c.Matrixs.TranslateV(this.Center.X, this.Center.Y)
                c.Matrixs.ScaleV(scaleFactor, scaleFactor)
                c.Matrixs.TranslateV(-this.Center.X, -this.Center.Y))
        else
            selectedRoutes |> Seq.iter(fun sr ->
                let center = sr.Matrixs.TransformPointW(PointF(sr.Width/2.f, sr.Height/2.f))
                sr.Matrixs.TranslateV(center.X, center.Y)
                sr.Matrixs.ScaleV(scaleFactor, scaleFactor)
                sr.Matrixs.TranslateV(-center.X, -center.Y))
        this.Invalidate()

    member this.Rotate(angle, isWorld) =
        if(isWorld) then
            this.LWControls |> Seq.iter(fun c ->
                c.Matrixs.TranslateV(this.Center.X, this.Center.Y)
                c.Matrixs.RotateV(angle)
                c.Matrixs.TranslateV(-this.Center.X, -this.Center.Y))
        else
            selectedRoutes |> Seq.iter(fun sr ->
                let center = sr.Matrixs.TransformPointW(PointF(sr.Width/2.f, sr.Height/2.f))
                sr.Matrixs.TranslateV(center.X, center.Y)
                sr.Matrixs.RotateV(angle)
                sr.Matrixs.TranslateV(-center.X, -center.Y))
                
        this.Invalidate()
        
    override this.OnMouseDown e =
        match this.Operation with
        | "NEW" ->
            tempRoute <- Some(new Route(Point2PointF(e.Location)))
            this.Operation <- "NEW_DRAW"
            this.Invalidate()
        | _ ->
              let oc = this.LWControls |> Seq.tryFindBack(fun c -> (c :?> Route).IsIn(c.Matrixs.TransformPointV(Point2PointF(e.Location))))
              //this.LWControls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location,(c :?> Route).Path.GetBounds()))
              match oc with
              | Some c ->
              movingRoute <- Some(c, PointF(single e.X - c.Left, single e.Y - c.Top))
              let p = c.Matrixs.TransformPointV(PointF(single e.X, single e.Y))
              let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
              c.OnMouseDown(evt)
              | None -> ()
        
        
        
        
    override this.OnMouseMove e =
        match this.Operation with
        | "NEW_DRAW" ->
            match tempRoute with
            | Some(r) ->
                r.AddPoint(Point2PointF(e.Location))
                tempRoute <- Some(r)
                this.Invalidate()
            | None -> ()
        | "MOVE" -> 
            match movingRoute with
            | Some(r, m) ->
                r.Position <- PointF(single e.X - m.X, single e.Y - m.Y)
                (r :?> Route).Pen.Color <- selectionColor
            | None -> 
                let oc = this.LWControls |> Seq.tryFindBack(fun c -> (c :?> Route).IsIn(c.Matrixs.TransformPointV(Point2PointF(e.Location))))
                match oc with
                | Some c -> 
                  let p = c.Matrixs.TransformPointV(PointF(single e.X, single e.Y))
                  let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
                  c.OnMouseMove(evt)
                | None -> ()
        | _ -> ()
        
    override this.OnMouseUp e =
        match this.Operation with
        | "NEW_DRAW" ->
            match tempRoute with
            | Some(r) ->
                r.EndRoute                    
                tempRoute <- None
                this.Operation <- "NEW"
                this.LWControls.Add(r)
                this.Invalidate()
            | None -> ()
        | _ -> ()
        movingRoute <- None
        base.OnMouseUp(e)
        
    override this.OnPaint(e) =
        match tempRoute with
        | Some(r) ->
            let t = e.Graphics.Transform
            e.Graphics.Transform <- r.Matrixs.WV
            r.OnPaint(e)
            e.Graphics.Transform <- t
        | None -> ()
        
        base.OnPaint(e)