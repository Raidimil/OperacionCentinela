using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Centinela : MonoBehaviour
{
    public static readonly string[] Escenas = {
        "00 Menu", "01 Galeria de disparos", "02 Hangar de drones", "03 Titan-01"
    };
    public int nivel;
    static int puntos, vidas;
    int municion;
    float angulo, espera, caida;
    float tiempoRestante;
    string resultado;
    CharacterController jugador;
    Camera vista;
    AudioSource sonidoArma;
    AudioClip pulso;
    static AudioSource musicaAmbiental;
    Transform arma;
    static readonly Vector3 posicionArma = new Vector3(.21f, -.13f, .55f);
    bool pausa;
    readonly List<Objetivo> objetivos = new List<Objetivo>();
    readonly List<Disparo> disparos = new List<Disparo>();
    readonly Dictionary<Color, Material> materiales = new Dictionary<Color, Material>();
    static readonly Color azul = new Color(.18f,.72f,1f);
    static readonly Color naranja = new Color(1f,.4f,.14f);
    static readonly Color acero = new Color(.26f,.34f,.42f);

    class Objetivo
    {
        public Transform cuerpo;
        public Collider impacto;
        public Collider contactoJefe;
        public int valor, salud;
        public float velocidad, altura, radioSeparacion;
    }

    class Disparo
    {
        public Transform rastro;
        public Vector3 direccion;
        public float tiempo;
    }

    void Start()
    {
        if (nivel == 0)
        {
            puntos = 0;
            vidas = 3;
        }
        else if (vidas <= 0) vidas = 3;

        tiempoRestante = nivel == 1 ? 30f : 60f;
        municion = nivel == 1 ? 14 : nivel == 2 ? 20 : 26;
        RenderSettings.ambientLight = new Color(.48f,.52f,.60f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.07f,.11f,.17f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 19;
        RenderSettings.fogEndDistance = 62;
        var luz = new GameObject("Luz principal").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.2f;
        luz.transform.rotation = Quaternion.Euler(48,-30,0);

        Pieza("Suelo", PrimitiveType.Cube, new Vector3(0,-.5f,6), new Vector3(24,1,40), acero);
        Pieza("Pared izquierda", PrimitiveType.Cube, new Vector3(-12,2,6), new Vector3(1,5,40), acero);
        Pieza("Pared derecha", PrimitiveType.Cube, new Vector3(12,2,6), new Vector3(1,5,40), acero);
        Pieza("Pared frontal", PrimitiveType.Cube, new Vector3(0,2,25), new Vector3(24,5,1), acero);
        Pieza("Pared trasera", PrimitiveType.Cube, new Vector3(0,2,-13), new Vector3(24,5,1), acero);
        Color franja = nivel == 3 ? new Color(.95f,.18f,.09f) : nivel == 2 ? naranja : azul;
        Color metal = new Color(.13f,.19f,.24f);
        for (int z = -10; z <= 22; z += 8)
        {
            foreach (int lado in new[] { -1, 1 })
            {
                float x = lado * 11.45f;
                Pieza("Contrafuerte", PrimitiveType.Cube, new Vector3(x,2.6f,z),
                    new Vector3(.7f,5.2f,.85f), metal, solido:false);
                Pieza("Banda de sector", PrimitiveType.Cube, new Vector3(x - lado * .38f,2.7f,z),
                    new Vector3(.09f,2.3f,.34f), franja, solido:false);
                Pieza("Carril de piso", PrimitiveType.Cube, new Vector3(lado * 8,.016f,z),
                    new Vector3(.12f,.025f,6), franja, solido:false);
            }
            Pieza("Viga del hangar", PrimitiveType.Cube, new Vector3(0,5.3f,z),
                new Vector3(23.5f,.45f,.85f), metal, solido:false);
            Pieza("Luz de techo", PrimitiveType.Cube, new Vector3(0,5.06f,z),
                new Vector3(5,.05f,.32f), Color.white, solido:false);
        }
        Pieza("Compuerta", PrimitiveType.Cube, new Vector3(0,2,24.4f), new Vector3(7,4,.2f), metal, solido:false);
        for (int i = -2; i <= 2; i++)
            Pieza("Panel de compuerta", PrimitiveType.Cube, new Vector3(i*1.25f,2,24.26f),
                new Vector3(1.13f,3.6f,.12f), acero, solido:false);

        jugador = new GameObject("Operador Centinela").AddComponent<CharacterController>();
        jugador.transform.position = new Vector3(0,.05f,-9);
        jugador.center = Vector3.up * .9f;
        jugador.height = 1.8f;
        jugador.radius = .35f;
        vista = new GameObject("Camara FPS").AddComponent<Camera>();
        vista.gameObject.AddComponent<AudioListener>();
        IniciarMusicaAmbiental();
        vista.transform.SetParent(jugador.transform, false);
        vista.transform.localPosition = Vector3.up * 1.58f;
        vista.clearFlags = CameraClearFlags.SolidColor;
        vista.backgroundColor = RenderSettings.fogColor;
        vista.nearClipPlane = .04f;
        if (nivel == 0)
        {
            vista.transform.localRotation = Quaternion.Euler(-8,-11,0);
            CrearProtagonistaMenu();
            CrearRobot(new Vector3(2,0,8), true);
            CrearRobot(new Vector3(-3,0,6), false);
            CursorDelJuego(false);
            return;
        }

        sonidoArma = vista.gameObject.AddComponent<AudioSource>();
        pulso = Resources.Load<AudioClip>("Audio/Pulso");
        arma = new GameObject("Blaster").transform;
        arma.SetParent(vista.transform, false);
        arma.localPosition = posicionArma;
        CrearPistola();
        Pieza("Guante del Operador Centinela", PrimitiveType.Cube, new Vector3(.035f,-.105f,-.035f),
            new Vector3(.09f,.085f,.13f), new Color(.09f,.14f,.18f), arma, false);

        if (nivel == 1)
            for (int i = 0; i < 9; i++) Diana(new Vector3((i % 3 - 1) * 5, 1.35f + (i / 3) * .83f, 3 + (i / 3) * 7), i / 3);
        if (nivel == 2)
            for (int i = 0; i < 7; i++) CrearRobot(new Vector3((i % 3 - 1) * 6,0,4 + i * 2.25f), false);
        if (nivel == 3)
        {
            CrearRobot(new Vector3(0,0,19), true);
            CrearRobot(new Vector3(-6,0,11), false);
            CrearRobot(new Vector3(6,0,11), false);
        }
        CursorDelJuego(true);
    }

    void IniciarMusicaAmbiental()
    {
        string recurso = nivel == 3 ? "Audio/Titan01" : "Audio/AmbienteCentinela";
        var pista = Resources.Load<AudioClip>(recurso);
        if (pista == null) return;

        if (musicaAmbiental == null)
        {
            var fondo = new GameObject("Musica ambiental");
            DontDestroyOnLoad(fondo);
            musicaAmbiental = fondo.AddComponent<AudioSource>();
            musicaAmbiental.spatialBlend = 0f;
            musicaAmbiental.volume = .65f;
            musicaAmbiental.loop = true;
        }
        if (musicaAmbiental.clip == pista && musicaAmbiental.isPlaying) return;
        musicaAmbiental.clip = pista;
        musicaAmbiental.Play();
    }

    void CrearProtagonistaMenu()
    {
        var modelo = new GameObject("Operador Centinela - modelo").transform;
        modelo.SetParent(vista.transform, false);
        modelo.localPosition = new Vector3(1.08f,-1.12f,3.05f);
        modelo.localRotation = Quaternion.Euler(8,155,0);
        var traje = new Color(.09f,.14f,.18f);
        Pieza("Torso", PrimitiveType.Cube, new Vector3(0,1.09f,0), new Vector3(.51f,.61f,.29f), traje, modelo, false);
        Pieza("Chaleco", PrimitiveType.Cube, new Vector3(0,1.15f,.13f), new Vector3(.42f,.43f,.13f), acero, modelo, false);
        Pieza("Insignia", PrimitiveType.Cube, new Vector3(.11f,1.24f,.202f), new Vector3(.13f,.04f,.018f), naranja, modelo, false);
        Pieza("Casco", PrimitiveType.Sphere, new Vector3(0,1.61f,0), new Vector3(.40f,.42f,.39f), acero, modelo, false);
        Pieza("Visor", PrimitiveType.Cube, new Vector3(0,1.63f,.17f), new Vector3(.31f,.12f,.08f), azul, modelo, false);
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Pieza("Manga", PrimitiveType.Cube, new Vector3(.33f * lado,1.08f,0), new Vector3(.16f,.49f,.19f), acero, modelo, false);
            Pieza("Guante", PrimitiveType.Cube, new Vector3(.33f * lado,.79f,.02f), new Vector3(.15f,.15f,.17f), traje, modelo, false);
            Pieza("Pierna", PrimitiveType.Cube, new Vector3(.14f * lado,.48f,0), new Vector3(.22f,.67f,.23f), traje, modelo, false);
            Pieza("Bota", PrimitiveType.Cube, new Vector3(.14f * lado,.12f,.04f), new Vector3(.23f,.23f,.32f), acero, modelo, false);
        }
    }

    void CrearPistola()
    {
        var modelo = Resources.Load<GameObject>("VoxelWeapons/Pistol");
        if (modelo == null)
        {
            Debug.LogError("Falta el modelo VoxelWeapons/Pistol.");
            return;
        }

        var pistola = Instantiate(modelo, Vector3.zero, Quaternion.identity);
        pistola.name = "Pistola de Texnist";
        var mallas = pistola.GetComponentsInChildren<Renderer>(true);
        if (mallas.Length == 0)
        {
            Debug.LogError("El FBX de la pistola no contiene mallas visibles.");
            Destroy(pistola);
            return;
        }

        foreach (var malla in mallas)
        {
            for (var nodo = malla.transform; nodo != null; nodo = nodo.parent)
                nodo.gameObject.SetActive(true);
            malla.enabled = true;
        }

        var paleta = Resources.Load<Texture2D>("VoxelWeapons/palette");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (paleta != null && shader != null)
        {
            paleta.filterMode = FilterMode.Point;
            var material = new Material(shader) { mainTexture = paleta, color = new Color(.74f, .76f, .80f) };
            material.SetFloat("_Smoothness", .18f);
            foreach (var malla in mallas)
            {
                var asignados = malla.sharedMaterials;
                for (int i = 0; i < asignados.Length; i++)
                    asignados[i] = material;
                malla.sharedMaterials = asignados;
            }
        }

        var dimensiones = mallas[0].bounds.size;
        if (dimensiones.x > dimensiones.z)
            pistola.transform.rotation = Quaternion.Euler(0, -94, 0);
        else
            pistola.transform.rotation = Quaternion.Euler(0, -4, 0);

        var limites = mallas[0].bounds;
        foreach (var malla in mallas)
            limites.Encapsulate(malla.bounds);

        float escala = .36f / Mathf.Max(limites.size.x, limites.size.y, limites.size.z, .0001f);
        var soporte = new GameObject("Modelo del arma").transform;
        pistola.transform.SetParent(soporte, true);
        soporte.SetParent(arma, false);
        soporte.localScale = Vector3.one * escala;
        soporte.localPosition = -limites.center * escala;
    }

    GameObject Pieza(string nombre, PrimitiveType tipo, Vector3 posicion, Vector3 escala, Color color,
        Transform padre = null, bool solido = true)
    {
        var objeto = GameObject.CreatePrimitive(tipo);
        objeto.name = nombre;
        objeto.transform.SetParent(padre, false);
        objeto.transform.localPosition = posicion;
        objeto.transform.localScale = escala;
        if (!materiales.TryGetValue(color, out var material))
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            materiales.Add(color, material);
        }
        objeto.GetComponent<Renderer>().sharedMaterial = material;
        if (!solido) objeto.GetComponent<Collider>().enabled = false;
        return objeto;
    }

    void Diana(Vector3 posicion, int tipo)
    {
        var color = tipo == 0 ? azul : tipo == 1 ? naranja : new Color(.94f,.25f,.48f);
        var centro = Pieza("Sonda " + (tipo + 1), PrimitiveType.Cube, posicion,
            new Vector3(1.45f,.96f,.6f) * (1.15f - tipo * .17f), acero);
        var raiz = centro.transform;
        Pieza("Visor", PrimitiveType.Cube, new Vector3(0,.04f,-.52f),
            new Vector3(.82f,.34f,.09f), color, raiz, false);
        foreach (int lado in new[] { -1, 1 })
            Pieza("Motor lateral", PrimitiveType.Cylinder, new Vector3(lado*.53f,0,0),
                new Vector3(.3f,.6f,.84f), color, raiz, false);
        centro.GetComponent<BoxCollider>().size = new Vector3(1.36f,1.2f,1.14f);
        objetivos.Add(new Objetivo { cuerpo = centro.transform, impacto = centro.GetComponent<Collider>(),
            valor = (tipo + 1) * 100, salud = 1 });
    }

    GameObject CrearModeloRobot(Transform padre, bool jefe)
    {
        var modelo = Resources.Load<GameObject>("RobotPack3D/BaseModel");
        if (modelo == null)
        {
            Debug.LogError("Falta RobotPack3D/BaseModel.dae en Resources.");
            return null;
        }

        var visual = Instantiate(modelo, padre, false);
        visual.name = jefe ? "Modelo Titán-01" : "Modelo Dron Vigía";
        visual.transform.localPosition = new Vector3(0, -0.65f, 0);
        visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        visual.transform.localScale = Vector3.one * (jefe ? 1.5f : 0.85f);

        var texturas = new[]
        {
            Resources.Load<Texture2D>("RobotPack3D/redTEX"),
            Resources.Load<Texture2D>("RobotPack3D/blueTEX"),
            Resources.Load<Texture2D>("RobotPack3D/greenTEX"),
            Resources.Load<Texture2D>("RobotPack3D/YellowTEX")
        };
        var colores = new[] { Color.red, azul, Color.green, naranja };
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        int indice = jefe ? 1 : 0;
        foreach (var renderer in renderers)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) continue;
            var material = new Material(shader) { color = colores[indice % colores.Length] };
            if (texturas[indice % texturas.Length] != null)
                material.mainTexture = texturas[indice % texturas.Length];
            renderer.material = material;
            indice++;
        }
        return visual;
    }
    void CrearRobot(Vector3 posicion, bool jefe)
    {
        var cuerpo = new GameObject(jefe ? "Titan-01" : "Dron Vigia").transform;
        cuerpo.position = posicion + Vector3.up * (jefe ? 1.45f : 1.25f);
        var blindaje = Pieza("Blindaje", PrimitiveType.Cube, Vector3.zero, jefe ? new Vector3(2,2.2f,.95f) :
            new Vector3(1.22f,.63f,.65f), acero, cuerpo, jefe);
        blindaje.GetComponent<Renderer>().enabled = false;
        var nucleo = Pieza("Nucleo", PrimitiveType.Sphere, new Vector3(0,.12f,jefe ? .57f : .31f),
            Vector3.one * (jefe ? .85f : .60f), naranja, cuerpo);
        nucleo.GetComponent<Renderer>().enabled = false;
        var visual = CrearModeloRobot(cuerpo, jefe);
        var impacto = nucleo.GetComponent<Collider>();
        float radioSeparacion = jefe ? 1.15f : .7f;
        if (visual != null)
        {
            var mallas = visual.GetComponentsInChildren<MeshFilter>();
            var limites = new Bounds();
            bool tieneLimites = false;

            foreach (var malla in mallas)
            {
                if (malla.sharedMesh == null) continue;

                var caja = malla.sharedMesh.bounds;
                var aCuerpo = cuerpo.worldToLocalMatrix * malla.transform.localToWorldMatrix;
                for (int esquina = 0; esquina < 8; esquina++)
                {
                    var vertice = new Vector3(
                        (esquina & 1) == 0 ? caja.min.x : caja.max.x,
                        (esquina & 2) == 0 ? caja.min.y : caja.max.y,
                        (esquina & 4) == 0 ? caja.min.z : caja.max.z);
                    var punto = aCuerpo.MultiplyPoint3x4(vertice);
                    if (tieneLimites)
                        limites.Encapsulate(punto);
                    else
                    {
                        limites = new Bounds(punto, Vector3.zero);
                        tieneLimites = true;
                    }
                }
            }

            if (tieneLimites)
            {
                var colisionCuerpo = cuerpo.gameObject.AddComponent<BoxCollider>();
                colisionCuerpo.center = limites.center;
                colisionCuerpo.size = limites.size + Vector3.one * .06f;
                colisionCuerpo.isTrigger = !jefe;
                var alcance = limites.extents + new Vector3(Mathf.Abs(limites.center.x), 0, Mathf.Abs(limites.center.z));
                radioSeparacion = new Vector2(alcance.x, alcance.z).magnitude + .08f;
                blindaje.GetComponent<Collider>().enabled = false;
                impacto.enabled = false;
                impacto = colisionCuerpo;
            }
        }

        objetivos.Add(new Objetivo { cuerpo = cuerpo, impacto = impacto,
            valor = jefe ? 1500 : 250, salud = jefe ? 7 : 2, velocidad = jefe ? 1.8f : 2.6f,
            contactoJefe = jefe ? (blindaje.GetComponent<Collider>().enabled ? blindaje.GetComponent<Collider>() : impacto) : null,
            altura = cuerpo.position.y, radioSeparacion = radioSeparacion });
    }

    void Update()
    {
        if (nivel == 0 || resultado != null) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pausa = !pausa;
            CursorDelJuego(!pausa);
        }
        if (pausa) return;
        float dt = Time.deltaTime;
        tiempoRestante = Mathf.Max(0f, tiempoRestante - dt);
        if (tiempoRestante <= 0f)
        {
            Terminar("Tiempo agotado");
            return;
        }

        jugador.transform.Rotate(0,Input.GetAxisRaw("Mouse X") * 2.3f,0);
        angulo = Mathf.Clamp(angulo - Input.GetAxisRaw("Mouse Y") * 2.3f,-78,78);
        vista.transform.localRotation = Quaternion.Euler(angulo,0,0);
        var local = new Vector3(
            ((Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) ? 1 : 0) - ((Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) ? 1 : 0), 0,
            ((Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) ? 1 : 0) - ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) ? 1 : 0));
        if (jugador.isGrounded && caida <= 0)
            caida = Input.GetKeyDown(KeyCode.Space) ? 5.8f : -2f;
        else
            caida -= 16f * dt;
        jugador.Move((jugador.transform.TransformDirection(local.normalized) * 5 + Vector3.up * caida) * dt);
        arma.localPosition = Vector3.Lerp(arma.localPosition, posicionArma, dt * 12);
        if (Input.GetMouseButtonDown(0) && municion > 0) Lanzar();

        AvanzarDisparos(dt);
        if (objetivos.Count == 0)
        {
            Terminar(nivel == 3 ? "Mision cumplida" : "Nivel completado");
            return;
        }
        if (municion == 0 && disparos.Count == 0)
        {
            Terminar("Sin municion");
            return;
        }

        espera -= dt;
        foreach (var objetivo in objetivos)
        {
            if (objetivo.velocidad == 0) continue;
            var destino = jugador.transform.position;
            destino.y = objetivo.altura;
            var cuerpo = objetivo.cuerpo;
            if ((destino - cuerpo.position).sqrMagnitude > .0025f)
                cuerpo.rotation = Quaternion.LookRotation(destino - cuerpo.position);
            if (objetivo.contactoJefe != null ? TocaAlJugador(objetivo.contactoJefe) : (destino - cuerpo.position).sqrMagnitude < 1.65f)
            {
                if (espera > 0) continue;
                vidas--;
                espera = 1.4f;
                if (vidas == 0)
                {
                    Terminar("Te quedaste sin vidas");
                    return;
                }
            }
            else MoverEnemigo(objetivo, destino, dt);
        }
    }

    bool TocaAlJugador(Collider blindaje)
    {
        Physics.SyncTransforms();
        var centro = jugador.transform.TransformPoint(jugador.center);
        float tramo = jugador.height * .5f - jugador.radius;
        var eje = centro;
        eje.y = Mathf.Clamp(blindaje.bounds.center.y, centro.y - tramo, centro.y + tramo);
        float alcance = jugador.radius + jugador.skinWidth;
        return (blindaje.ClosestPoint(eje) - eje).sqrMagnitude <= alcance * alcance;
    }

    void MoverEnemigo(Objetivo objetivo, Vector3 destino, float dt)
    {
        var origen = objetivo.cuerpo.position;
        float paso = objetivo.velocidad * dt;
        var siguiente = Vector3.MoveTowards(origen, destino, paso);
        if (HayEspacio(objetivo, siguiente))
        {
            objetivo.cuerpo.position = siguiente;
            return;
        }

        var lateral = Vector3.Cross(Vector3.up, (destino - origen).normalized) * paso;
        if (HayEspacio(objetivo, origen + lateral))
            objetivo.cuerpo.position = origen + lateral;
        else if (HayEspacio(objetivo, origen - lateral))
            objetivo.cuerpo.position = origen - lateral;
    }

    bool HayEspacio(Objetivo objetivo, Vector3 posicion)
    {
        foreach (var otro in objetivos)
        {
            if (otro == objetivo || otro.velocidad == 0) continue;

            var distancia = posicion - otro.cuerpo.position;
            distancia.y = 0;
            float minimo = objetivo.radioSeparacion + otro.radioSeparacion;
            if (distancia.sqrMagnitude < minimo * minimo)
                return false;
        }
        return true;
    }

    void Lanzar()
    {
        municion--;
        if (pulso != null) sonidoArma.PlayOneShot(pulso, .85f);
        arma.localPosition -= Vector3.forward * .10f;
        var origen = vista.transform.position + vista.transform.forward * .55f;
        var rastro = Pieza("Proyectil", PrimitiveType.Sphere, origen, Vector3.one * .11f,
            naranja, solido:false);
        disparos.Add(new Disparo { rastro = rastro.transform, direccion = vista.transform.forward, tiempo = 1.2f });
    }

    void AvanzarDisparos(float dt)
    {
        for (int i = disparos.Count - 1; i >= 0; i--)
        {
            var disparo = disparos[i];
            float distancia = 34 * dt;
            bool impacto = Physics.Raycast(disparo.rastro.position, disparo.direccion, out var golpe, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (impacto)
            {
                int indice = objetivos.FindIndex(objetivo => objetivo.impacto == golpe.collider);
                if (indice >= 0)
                {
                    var objetivo = objetivos[indice];
                    objetivo.salud--;
                    if (objetivo.salud == 0)
                    {
                        puntos += objetivo.valor;
                        foreach (var collider in objetivo.cuerpo.GetComponentsInChildren<Collider>()) collider.enabled = false;
                        Destroy(objetivo.cuerpo.gameObject);
                        objetivos.RemoveAt(indice);
                    }
                }
            }
            disparo.tiempo -= dt;
            if (impacto || disparo.tiempo <= 0)
            {
                Destroy(disparo.rastro.gameObject);
                disparos.RemoveAt(i);
            }
            else disparo.rastro.position += disparo.direccion * distancia;
        }
    }

    void Terminar(string texto)
    {
        resultado = texto;
        CursorDelJuego(false);
    }

    void CursorDelJuego(bool capturar)
    {
        Cursor.lockState = capturar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !capturar;
    }

    void OnGUI()
    {
        float escala = Mathf.Min(Screen.width / 1000f, Screen.height / 600f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1000 * escala) / 2, (Screen.height - 600 * escala) / 2, 0),
            Quaternion.identity, Vector3.one * escala);
        var titulo = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
        var normal = new GUIStyle(GUI.skin.label) { fontSize = 17 };
        if (nivel == 0 || resultado != null || pausa)
        {
            var panel = new Rect(50,165,520,275);
            GUI.color = new Color(.07f,.12f,.19f,.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(75,183,485,50), nivel == 0 ? "OPERACION CENTINELA" : pausa ? "Pausa" : resultado, titulo);
            string detalle = nivel == 0 ? "Operador Centinela\nWASD o flechas para moverte | Raton para apuntar\nEspacio para saltar | Clic izquierdo para disparar" :
                "Puntos: " + puntos + "    Vidas: " + vidas;
            GUI.Label(new Rect(76,245,470,60), detalle, normal);
            bool completo = resultado == "Nivel completado";
            string boton = nivel == 0 ? "Jugar" : pausa ? "Continuar" : completo ? "Siguiente nivel" : "Volver a jugar";
            if (GUI.Button(new Rect(76,322,220,43), boton))
            {
                if (pausa) { pausa = false; CursorDelJuego(true); }
                else if (completo) SceneManager.LoadScene(Escenas[nivel + 1]);
                else
                {
                    puntos = 0;
                    vidas = 3;
                    SceneManager.LoadScene(Escenas[1]);
                }
            }
            if (nivel != 0 && GUI.Button(new Rect(312,322,170,43), "Menu principal"))
                SceneManager.LoadScene(Escenas[0]);
            return;
        }
        GUI.Box(new Rect(18,18,296,112), "");
        GUI.Label(new Rect(30,24,270,25), Escenas[nivel], normal);
        GUI.Label(new Rect(30,55,275,45), "Puntos: " + puntos + "    Vidas: " + vidas +
            "\nMunicion: " + municion + "    Objetivos: " + objetivos.Count, normal);
        int segundos = Mathf.CeilToInt(tiempoRestante);
        var colorAnterior = GUI.contentColor;
        if (segundos <= 10) GUI.contentColor = naranja;
        GUI.Label(new Rect(30,99,270,24), $"Tiempo: {segundos / 60:00}:{segundos % 60:00}", normal);
        GUI.contentColor = colorAnterior;
        GUI.DrawTexture(new Rect(491,299,18,2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(499,291,2,18), Texture2D.whiteTexture);
    }
}



