# Instructivo para AI Frontend - GestionTareas Angular

## Contexto
El backend ya esta construido y corriendo. Tu trabajo es crear el frontend Angular
que se conecta a el. No debes tocar nada del backend.

---

## Backend disponible
- URL base: http://localhost:5050
- Swagger UI: http://localhost:5050 (puedes probar los endpoints ahi)
- CORS: abierto para cualquier origen

---

## Autenticacion JWT (OBLIGATORIO)

El backend usa JWT Bearer. Todos los endpoints excepto /api/auth/* requieren token.

### Flujo de autenticacion

1. Usuario hace POST /api/auth/register o POST /api/auth/login
2. Backend devuelve un JWT con duracion de 8 horas
3. Angular guarda el token en localStorage
4. Cada request a la API lleva el header: Authorization: Bearer <token>
5. Si el backend devuelve 401, redirigir al login

### Endpoints publicos (sin token)

POST /api/auth/register
Body: { "nombre": "Carlos Perez", "email": "carlos@empresa.com", "password": "mi_clave_123" }
Respuesta 201: { "token": "eyJ...", "nombre": "Carlos Perez", "email": "carlos@empresa.com", "rol": "User", "expiracion": "2024-..." }

POST /api/auth/login
Body: { "email": "carlos@empresa.com", "password": "mi_clave_123" }
Respuesta 200: { "token": "eyJ...", "nombre": "Carlos Perez", "email": "carlos@empresa.com", "rol": "User", "expiracion": "2024-..." }

### Endpoints protegidos (requieren Bearer token)

GET    /api/users
POST   /api/tasks
GET    /api/tasks
PUT    /api/tasks/{id}/status

---

## Tecnologias del frontend
- Angular 17+ standalone components
- TypeScript
- Reactive Forms
- HttpClient + HttpInterceptor para el token
- Angular Router con AuthGuard

---

## Modelos TypeScript

// models/auth.model.ts
export interface RegisterRequest { nombre: string; email: string; password: string; }
export interface LoginRequest    { email: string; password: string; }
export interface AuthResponse    { token: string; nombre: string; email: string; rol: string; expiracion: string; }

// models/user.model.ts
export interface User { id: number; nombre: string; email: string; fechaCreacion: string; }

// models/task.model.ts
export type TaskStatus = 'Pending' | 'InProgress' | 'Done';
export interface Task {
  id: number; titulo: string; descripcion?: string;
  estado: TaskStatus; usuarioId: number; usuarioNombre: string;
  fechaCreacion: string; fechaActualizacion?: string; infoAdicional?: string;
}
export interface CreateTaskRequest { titulo: string; descripcion?: string; usuarioId: number; infoAdicional?: string; }
export interface ChangeStatusRequest { nuevoEstado: TaskStatus; }

---

## Environment

// environments/environment.ts
export const environment = { production: false, apiUrl: 'http://localhost:5050' };

---

## AuthService Angular

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly url = environment.apiUrl + '/api/auth';

  constructor(private http: HttpClient, private router: Router) {}

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url + '/register', req).pipe(
      tap(res => localStorage.setItem('token', res.token))
    );
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(this.url + '/login', req).pipe(
      tap(res => localStorage.setItem('token', res.token))
    );
  }

  logout() { localStorage.removeItem('token'); this.router.navigate(['/login']); }
  getToken(): string | null { return localStorage.getItem('token'); }
  isLoggedIn(): boolean { return !!this.getToken(); }
}

---

## HTTP Interceptor (envia token en cada request)

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.auth.getToken();
    if (token) {
      req = req.clone({ setHeaders: { Authorization: 'Bearer ' + token } });
    }
    return next.handle(req).pipe(
      catchError(err => {
        if (err.status === 401) {
          this.auth.logout(); // token expirado o invalido
        }
        return throwError(() => err);
      })
    );
  }
}

// Registrar en app.config.ts o app.module.ts:
// { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }

---

## AuthGuard

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}
  canActivate(): boolean {
    if (this.auth.isLoggedIn()) return true;
    this.router.navigate(['/login']);
    return false;
  }
}

---

## TaskService Angular

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly url = environment.apiUrl + '/api/tasks';
  constructor(private http: HttpClient) {}

  getAll(usuarioId?: number, estado?: TaskStatus): Observable<Task[]> {
    let params = new HttpParams();
    if (usuarioId) params = params.set('usuarioId', usuarioId.toString());
    if (estado)    params = params.set('estado', estado);
    return this.http.get<Task[]>(this.url, { params });
  }

  create(req: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(this.url, req);
  }

  changeStatus(id: number, nuevoEstado: TaskStatus): Observable<Task> {
    return this.http.put<Task>(this.url + '/' + id + '/status', { nuevoEstado });
  }
}

---

## UserService Angular

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly url = environment.apiUrl + '/api/users';
  constructor(private http: HttpClient) {}
  getAll(): Observable<User[]> { return this.http.get<User[]>(this.url); }
}

---

## Rutas Angular

/login          -> LoginComponent      (publica)
/register       -> RegisterComponent   (publica)
/tasks          -> TaskListComponent   (requiere AuthGuard)
/tasks/new      -> TaskFormComponent   (requiere AuthGuard)

Redirigir / a /tasks si esta logueado, a /login si no.

---

## Vistas requeridas

### Vista 1 - Login (/login)
- Formulario reactivo: email + password
- Boton Login -> POST /api/auth/login
- Link a /register
- Guardar token en localStorage
- Redirigir a /tasks al exito
- Mostrar error si credenciales incorrectas

### Vista 2 - Register (/register)
- Formulario reactivo: nombre + email + password
- Boton Registrar -> POST /api/auth/register
- Link a /login
- Al exito: guardar token + redirigir a /tasks

### Vista 3 - Lista de tareas (/tasks) [protegida]
- Tabla o cards con todas las tareas
- Cada tarea: titulo, descripcion, estado (badge coloreado), usuario, fecha
- Filtro por estado: Todos / Pending / InProgress / Done
- Boton cambiar estado en cada tarea
- Boton Nueva tarea -> /tasks/new
- Boton Logout en el header
- Colores: Pending=amarillo, InProgress=azul, Done=verde

### Vista 4 - Nueva tarea (/tasks/new) [protegida]
- Titulo (obligatorio)
- Descripcion (opcional)
- Select usuario (carga GET /api/users)
- Prioridad (opcional, se serializa como JSON en infoAdicional)
- Al guardar: redirigir a /tasks

---

## Reglas de negocio en el frontend

1. NO mostrar opcion Done si la tarea esta en Pending
2. El select de usuario carga datos reales de GET /api/users
3. infoAdicional se construye asi:
   infoAdicional = JSON.stringify({ prioridad: form.value.prioridad })

---

## Manejo de errores

- 400: mostrar campo error del JSON inline bajo el formulario
- 401: redirigir a /login (el interceptor lo hace automatico)
- 404: mostrar mensaje No encontrado
- 500: mostrar Error del servidor, intente de nuevo

---

## Prueba end-to-end esperada

1. /register -> crear cuenta con email y password
2. /login    -> autenticarse (recibir token)
3. /tasks    -> ver lista vacia
4. /tasks/new -> crear tarea asignada al usuario registrado
5. Ver tarea con estado Pending
6. Cambiar a InProgress
7. Cambiar a Done
8. Filtrar por Done -> solo aparece esa tarea
9. Logout -> redirige a /login
10. Intentar acceder a /tasks sin token -> redirige a /login