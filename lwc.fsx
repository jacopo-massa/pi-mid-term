#nowarn
open System.Drawing
open System.Drawing.Drawing2D
open System.Windows.Forms

// Libreria
let Point2PointF (p:Point) = new PointF(single p.X, single p.Y)

type WVMatrix () =
  let wv = new Drawing2D.Matrix()
  let vw = new Drawing2D.Matrix()

  member this.TranslateW (tx, ty) =
    wv.Translate(tx, ty)
    vw.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleW (sx, sy) =
    wv.Scale(sx, sy)
    vw.Scale(1.f /sx, 1.f/ sy, Drawing2D.MatrixOrder.Append)

  member this.RotateW (a) =
    wv.Rotate(a)
    vw.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.RotateV (a) =
    vw.Rotate(a)
    wv.Rotate(-a, Drawing2D.MatrixOrder.Append)

  member this.TranslateV (tx, ty) =
    vw.Translate(tx, ty)
    wv.Translate(-tx, -ty, Drawing2D.MatrixOrder.Append)

  member this.ScaleV (sx, sy) =
    vw.Scale(sx, sy)
    wv.Scale(1.f /sx, 1.f/ sy, Drawing2D.MatrixOrder.Append)
  
  member this.TransformPointV (p:PointF) =
    let a = [| p |]
    vw.TransformPoints(a)
    a.[0]

  member this.TransformPointW (p:PointF) =
    let a = [| p |]
    wv.TransformPoints(a)
    a.[0]

  member this.VW with get() = vw
  member this.WV with get() = wv

 type LWControl() as this =
  let matrixs = WVMatrix()

  let mutable size = SizeF(0.f, 0.f)
  let mutable position = PointF()

  let mutable parent : LWContainer option = None

  member this.Matrixs with get() = matrixs

  member this.Parent
    with get() = parent
    and set(v) = parent <- v

  abstract OnPaint : PaintEventArgs -> unit
  default this.OnPaint (e) = ()

  abstract OnMouseDown : MouseEventArgs -> unit
  default this.OnMouseDown (e) = ()

  abstract OnMouseUp : MouseEventArgs -> unit
  default this.OnMouseUp (e) = ()

  abstract OnMouseMove : MouseEventArgs -> unit
  default this.OnMouseMove (e) = ()

  member this.Invalidate() =
    match parent with
    | Some p -> p.Invalidate()
    | None -> ()
  member this.HitTest(p:Point) =
    let pt = matrixs.TransformPointV(PointF(single p.X, single p.Y))
    let boundingBox = RectangleF(0.f, 0.f, size.Width, size.Height)
    boundingBox.Contains(pt)

  member this.Size
    with get() = size
    and set(v) = 
      size <- v
      this.Invalidate()

  member this.Position
    with get() = position
    and set(v:PointF) =
      matrixs.TranslateV(position.X, position.Y) // traslo la vista
      position <- v //aggiorno la posizione
      matrixs.TranslateV(-position.X, -position.Y) // riporto apposto
      this.Invalidate()

  member this.PositionInt with get() = Point(int position.X, int position.Y)
  member this.SizeInt with get() = Size(int size.Width, int size.Height)

  member this.Left
    with get() = position.X
    and set(v) = position.X <- v
  member this.Top
    with get() = position.Y
    and set(v) = position.Y <- v
  member this.Width
    with get() = size.Width
    and set(v) = size.Width <- v
  member this.Height
    with get() = size.Height
    and set(v) = size.Height <- v

and LWContainer() as this =
  inherit UserControl()

  let controls = System.Collections.ObjectModel.ObservableCollection<LWControl>()
  let matrix = WVMatrix()

  do
    (* entrambi metodi per evitare il flickering *)
    this.SetStyle(ControlStyles.OptimizedDoubleBuffer ||| ControlStyles.AllPaintingInWmPaint, true)
    controls.CollectionChanged.Add(fun e ->
      if(e.NewItems <> null) then
        for i in e.NewItems do
          (i :?> LWControl).Parent <- Some(this : LWContainer))
  done

  member this.LWControls with get() = controls
  member this.Matrix with get() = matrix
  
  member this.ContainerWidth with get() = this.Width
  
  member this.ContainerHeight with get() = this.Height

  override this.OnMouseDown (e) =
    let oc =
      controls |> Seq.tryFindBack(fun c ->
        c.HitTest(e.Location))
    match oc with
    | Some c ->
      let p = c.Matrixs.TransformPointV(PointF(single e.X, single e.Y))
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseDown(evt)
    | None -> ()
    
  override this.OnResize e =
    this.Invalidate()

  override this.OnMouseUp (e) =
     controls |> Seq.iter(fun c ->
      let p = c.Matrixs.TransformPointV(PointF(single e.X, single e.Y))
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseUp(evt))

  override this.OnMouseMove (e) =
    let oc =
      controls |> Seq.tryFindBack(fun c -> c.HitTest(e.Location))
    match oc with
    | Some c ->
      let p = c.Matrixs.TransformPointV(PointF(single e.X, single e.Y))
      let evt = new MouseEventArgs(e.Button, e.Clicks, int p.X, int p.Y, e.Delta)
      c.OnMouseMove(evt)
    | None -> ()
    

  override this.OnPaint(e) =
    controls 
    |> Seq.iter(fun c ->
      e.Graphics.SmoothingMode <- SmoothingMode.AntiAlias
      e.Graphics.Transform <- c.Matrixs.WV
      c.OnPaint(e))

    
