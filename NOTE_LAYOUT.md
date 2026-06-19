# NOTE_LAYOUT — Layout, Shell, Route

เอกสารนี้สรุปแนวทาง **prompt** เพื่อให้ได้ระบบจัดการ Layout / Shell / Route แบบเดียวกับ Codebase นี้ และสรุปสิ่งที่ทำพร้อมไฟล์สำคัญที่เกี่ยวข้อง

---

## 1. แนวทาง Prompt เพื่อให้ได้ระบบ Layout, Shell, Route แบบ Codebase นี้

### 1.1 Prompt หลัก (ใช้เมื่อเริ่มโครงสร้างใหม่)

```
สร้างระบบ Layout สำหรับ Angular standalone app แบบ Shell + Child Routes:

- app-root มีแค่ <router-outlet /> ไม่ใส่ sidebar/header ที่ root
- route /login เป็น public ไม่มี shell (LoginComponent แยกเต็มหน้า)
- route '' ใช้ AppShellComponent เป็น layout wrapper พร้อม canActivate: [authGuard]
- child routes ของ shell: '' → HomeComponent, 'machine' → MachineComponent
- wildcard ** redirect ไป ''
- หน้า feature (home, machine) เป็น content-only ไม่มี sidebar/topbar ของตัวเอง
- shell มี sidebar (nav items), topbar (username, role badge, logout), และ <router-outlet /> ใน content area
- nav items กำหนดเป็น array ใน shell component พร้อม routerLink + routerLinkActive
- sidebar collapse/expand เก็บ preference ใน localStorage
- ใช้ inject(AuthService) ใน shell สำหรับ currentUser, isAdmin, logout
- global styles ใน styles.css ใช้ Tailwind + shared component classes (.btn, .form-input, .alert)
- ใช้ @ng-icons/core สำหรับ icon ทั้งระบบ (ไม่ใช้ emoji) — ลงทะเบียนใน app.config.ts ผ่าน provideIcons()
- navItems เก็บ icon เป็น AppIconName (เช่น 'heroHome') และ render ด้วย <ng-icon>
- เขียน unit test สำหรับ routes, shell component, และ app root
```

### 1.2 Prompt เมื่อเพิ่มหน้าใหม่

```
เพิ่มหน้า [ชื่อหน้า] ใน Angular app ที่ใช้ AppShell pattern:

1. สร้าง [feature]/[feature].component.ts เป็น standalone content-only (ไม่มี layout ซ้ำ)
2. เพิ่ม child route ใน app.routes.ts ภายใต้ shell route (path: '' + canActivate: [authGuard])
3. เพิ่ม nav item ใน AppShellComponent.navItems (label, icon, route)
4. อัปเดต app.routes.spec.ts ให้ตรวจ child path ใหม่
5. อัปเดต app-shell.component.spec.ts ให้ตรวจจำนวน nav links
```

### 1.3 Prompt เมื่อปรับ Shell / Navigation

```
ปรับ AppShell ใน Angular app:

- sidebar: fixed left, dark theme, collapse ได้ด้วย CSS variable (--sidebar-width, --sidebar-collapsed-width)
- topbar: สูง --topbar-height, มี toggle sidebar, user info, logout button
- content-area: margin-left ตาม sidebar width, padding สำหรับ page content
- ใช้ @for + routerLink + routerLinkActive ใน sidebar
- route '/' ใช้ routerLinkActiveOptions exact: true
- aria-label / aria-expanded สำหรับ accessibility
```

### 1.4 Prompt เมื่อเชื่อม Auth กับ Route

```
เชื่อม Auth กับ Routing แบบ Codebase นี้:

- authGuard (CanActivateFn): ถ้า isLoggedIn() → true, ไม่งั้น initializeSession() แล้ว redirect /login
- ใส่ authGuard เฉพาะ shell route ไม่ใส่ที่ /login
- LoginComponent: ถ้า login สำเร็จหรือ session init สำเร็จ → navigate(['/'])
- logout ใน shell เรียก authService.logout() ซึ่ง clear session และ navigate /login
- หน้า feature อ่าน currentUser จาก AuthService ได้ แต่ไม่จัดการ redirect เอง
```

### 1.5 หลักการสำคัญที่ควรระบุใน Prompt

| หลักการ | รายละเอียด |
|---------|------------|
| Single Shell | Layout (sidebar + topbar) อยู่ที่ `AppShellComponent` ที่เดียว |
| Nested Outlet | Root outlet → Shell หรือ Login / Shell outlet → Feature pages |
| Public vs Protected | `/login` ไม่มี guard / Shell + children มี `authGuard` |
| Content-only Pages | Feature component มีแค่เนื้อหาหน้า (เช่น `.page-container`) |
| Nav ใน Shell | เมนูไม่กระจายไปแต่ละหน้า แก้ที่ `navItems` จุดเดียว |
| Global vs Local CSS | Tailwind utilities ใน template + shared classes ใน `styles.css` (@layer components) / layout ใน shell CSS / page-specific CSS เฉพาะ animation หรือ logic ที่ซับซ้อน |
| Icons | ใช้ `@ng-icons/core` + `<ng-icon>` — อ้างชื่อจาก `APP_ICONS` ใน `shared/app-icons.ts` |

### 1.6 ตัวอย่าง Prompt สั้น (copy-paste ได้)

```
สร้าง AppShellComponent (sidebar + topbar + router-outlet) และจัด app.routes.ts ให้ /login เป็น public, shell route มี authGuard และ children home/machine, wildcard redirect ''
```

```
เพิ่ม route /reports พร้อม ReportsComponent เป็น child ของ shell และเพิ่มเมนูใน sidebar
```

---

## 2. สรุปสิ่งที่ทำ และไฟล์สำคัญที่เกี่ยวข้อง

### 2.1 สิ่งที่ทำ (Architecture Overview)

```
app-root (<router-outlet />)
├── /login          → LoginComponent        (public, ไม่มี shell)
└── /               → AppShellComponent     (protected โดย authGuard)
    ├── /           → HomeComponent         (child outlet ใน shell)
    └── /machine    → MachineComponent
/**                 → redirect ไป /
```

**App Root — บางที่สุด**
- `App` component มีแค่ `<router-outlet />` ไม่รับผิดชอบ layout

**App Shell — Layout หลัก**
- Sidebar: เมนูนำทาง (หน้าหลัก, Machine), collapse/expand, จำ state ใน `localStorage` (`app-sidebar-collapsed`)
- Topbar: ปุ่ม toggle sidebar, แสดง username + role badge, ปุ่ม logout
- Content area: `<router-outlet />` สำหรับ render หน้า feature

**Routing**
- Shell route (`path: ''`) ครอบ child routes ทั้งหมดที่ต้อง login
- `authGuard` ตรวจ session ก่อนเข้า shell; ไม่ผ่าน → `/login`
- Wildcard redirect unknown paths กลับ entry route

**Feature Pages**
- `HomeComponent`, `MachineComponent` เป็น standalone content-only
- Machine มี `.page-container`, `.page-header` ของตัวเอง ไม่ duplicate shell

**Global Styles**
- `styles.css`: Tailwind entry (`@import 'tailwindcss'`), base reset, shared component classes (`.btn`, `.form-input`, `.alert`, spinners, icon sizing utilities)

### 2.2 ไฟล์สำคัญ

#### Routing & Bootstrap

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/app/app.routes.ts` | กำหนด routes: login (public), shell + children (protected), wildcard |
| `frontend/src/app/app.config.ts` | `provideRouter(routes)` + HTTP client / interceptor |
| `frontend/src/app/app.ts` | Root component (standalone) |
| `frontend/src/app/app.html` | `<router-outlet />` ระดับ root |
| `frontend/src/app/app.css` | Styles ของ root (ว่าง — layout อยู่ที่ shell) |

#### Shell / Layout

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/app/layout/app-shell.component.ts` | Shell logic: navItems, sidebar collapse, auth integration |
| `frontend/src/app/layout/app-shell.component.html` | Sidebar, topbar, nested `<router-outlet />` |
| `frontend/src/app/layout/app-shell.component.css` | Layout CSS (sidebar fixed, topbar, content-area, responsive collapse) |
| `frontend/src/app/layout/app-shell.component.spec.ts` | Test shell: user display, sidebar toggle, nav links, logout |

#### Feature Pages (Child Routes)

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/app/home/home.component.ts` | หน้าหลัก — อ่าน `currentUser` จาก AuthService |
| `frontend/src/app/home/home.component.html` | Welcome card |
| `frontend/src/app/home/home.component.css` | Styles เฉพาะหน้า home |
| `frontend/src/app/machine/machine.component.ts` | หน้า Machine Management |
| `frontend/src/app/machine/machine.component.html` | เนื้อหา CRUD / search (content-only) |
| `frontend/src/app/machine/machine.component.css` | Styles เฉพาะหน้า machine |

#### Auth ที่เกี่ยวกับ Route (ไม่ใช่ layout แต่ผูกกับ shell)

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/app/auth/auth.guard.ts` | ป้องกัน shell route — session init หรือ redirect `/login` |
| `frontend/src/app/auth/login/login.component.ts` | Public login — navigate `/` เมื่อสำเร็จ |
| `frontend/src/app/auth/auth.service.ts` | `currentUser`, `isAdmin`, `logout()` (redirect login) |

#### Global Styles

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/styles.css` | Tailwind + global shared component classes |
| `frontend/postcss.config.json` | PostCSS plugin สำหรับ Tailwind v4 |
| `frontend/src/app/shared/app-icons.ts` | Semantic icon map + registry สำหรับ `provideIcons()` |

#### Tests

| ไฟล์ | บทบาท |
|------|--------|
| `frontend/src/app/app.routes.spec.ts` | ตรวจ login public, shell มี guard + children, wildcard redirect |
| `frontend/src/app/app.spec.ts` | ตรวจ root render `<router-outlet />` |

### 2.3 โครงสร้าง Route ใน Code (อ้างอิง)

```typescript
// app.routes.ts — โครงสร้างหลัก
export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    {
        path: '',
        component: AppShellComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: HomeComponent },
            { path: 'machine', component: MachineComponent },
        ],
    },
    { path: '**', redirectTo: '' },
];
```

### 2.4 จุดที่มักแก้เมื่อขยายระบบ

1. **เพิ่มหน้าใหม่** → สร้าง component + เพิ่ม `children` ใน `app.routes.ts` + เพิ่ม `navItems` ใน shell
2. **เปลี่ยนเมนู** → แก้ `navItems` ใน `app-shell.component.ts` เท่านั้น
3. **เปลี่ยน layout ทั้ง app** → แก้ shell HTML/CSS จุดเดียว
4. **หน้า public ใหม่** (ไม่ต้อง login) → เพิ่ม route แยกนอก shell คล้าย `/login`
5. **Role-based menu** → filter `navItems` ใน shell จาก `isAdmin()` หรือ role อื่น

### 2.5 คำถามที่ควรถามตัวเอง (Checklist หลัง implement)

1. Root มีแค่ `<router-outlet />` หรือยังใส่ layout ซ้ำที่ root?
2. หน้า login อยู่นอก shell และไม่มี `authGuard` หรือไม่?
3. Shell route มี `authGuard` และ child ใช้ nested `<router-outlet />` หรือไม่?
4. Feature page เป็น content-only ไม่ duplicate sidebar/topbar หรือไม่?
5. เมนู sidebar sync กับ routes ใน `app.routes.ts` หรือไม่?
6. Wildcard redirect กลับ entry route หรือไม่?
7. มี test ครอบ routes structure และ shell behavior หรือไม่?

---

*เอกสารนี้อ้างอิงจากโครงสร้างปัจจุบันของ Basic App (Angular standalone + AppShell pattern)*


---

## 3. คำอธิบาย Sidebar Navigation Structure

### 3.1 Overview

Sidebar navigation ใน `app-shell.component.html` ใช้ **@for loop** เพื่อ render menu items แบบ dynamic จาก `navItems` array ที่สร้างใน `app-shell.component.ts`

```html
<nav class="sidebar-nav">
    @for (item of navItems; track item.route) {
    <a
        class="nav-item"
        [routerLink]="item.route"
        routerLinkActive="active"
        [routerLinkActiveOptions]="{ exact: item.route === '/' }"
        [attr.aria-label]="item.label"
        [title]="sidebarCollapsed() ? item.label : null"
    >
        <ng-icon class="nav-icon icon-nav" [name]="item.icon" aria-hidden="true" />
        <span class="nav-label" [class.hidden-label]="sidebarCollapsed()">{{ item.label }}</span>
    </a>
    }
</nav>
```

---

### 3.2 โครงสร้าง HTML — Element ต่อ Element

#### Container: `<nav class="sidebar-nav">`
- **ทำหน้าที่**: Semantic landmark สำหรับ screen readers ว่านี่คือพื้นที่นำทาง
- **Class**: `sidebar-nav` — ใช้สำหรับ CSS styling และ responsive design
- **บทบาท**: ครอบหมด menu items ทั้งหมด

#### Loop: `@for (item of navItems; track item.route)`
- **ทำหน้าที่**: Angular control flow directive สำหรับ loop array `navItems`
- **`track item.route`**: บอก Angular ให้ติดตาม item ด้วย property `route` เพื่อ optimize re-rendering
  - ถ้าไม่มี `track`, Angular อาจ re-render ทุก item เมื่อ navItems เปลี่ยน
  - ใช้ unique identifier (`route`) ทำให้ performance ดีขึ้น
- **Loop variable**: `item` — object แต่ละอัน ที่มี properties เช่น `route`, `label`, `icon`

#### Link Item: `<a class="nav-item" ...>`
- **Element**: `<a>` tag — anchor element สำหรับนำทาง (ความหมายสำคัญสำหรับ SEO และ accessibility)
- **Class `nav-item`**: Styling และ differentiate จาก other `<a>` tags ใน app
  - เมื่อ active: CSS class `active` ถูกเพิ่มโดย `routerLinkActive="active"` (ดู attribute ด้านล่าง)

---

### 3.3 Angular Directives & Bindings

#### `[routerLink]="item.route"`
- **ประเภท**: Property binding
- **ทำหน้าที่**: กำหนด route ที่ link นี้ชี้ไป
- **ตัวอย่าง**: 
  - `item.route = '/'` → `<a routerLink="/">`
  - `item.route = 'machine'` → `<a routerLink="machine">`
- **ผลลัพธ์**: เมื่อคลิก link, Angular Router navigate ไปที่ path นั้น

#### `routerLinkActive="active"`
- **ประเภท**: Directive
- **ทำหน้าที่**: ตรวจ active route และเพิ่ม CSS class ชื่อ `active` ให้กับ `<a>` tag นั้น
- **ตัวอย่าง**: 
  - ถ้า route ปัจจุบัน = `/` และ link `routerLink="/"` → ได้ `class="nav-item active"`
  - `<a class="nav-item active">...</a>` — ปรกติ CSS มี `.nav-item.active { background-color: ...; }` เพื่อแสดงว่า active

#### `[routerLinkActiveOptions]="{ exact: item.route === '/' }"`
- **ประเภท**: Property binding
- **ทำหน้าที่**: ปรับวิธีการ match route ของ `routerLinkActive`
  - `exact: true` → match exact route path เท่านั้น (เคร่งครัด)
    - ตัวอย่าง: home link (`/`) ต้องคลิก route `/` ถึงจะ active ไม่ได้ activate เมื่อ route = `/machine`
  - `exact: false` (default) → match prefix (ด้าน prefix match)
    - ตัวอย่าง: link `routerLink="/"` active ทั้งเมื่อ route `/` และ `/machine` (เพราะ `/machine` มี `/` เป็นต้น)
- **การตัดสินใจในคำสั่ง**: `item.route === '/'` 
  - ถ้า item route = `/` (home) → `exact: true` ✓ (home link active เฉพาะที่ `/`)
  - ถ้า item route = `machine` → `exact: false` (machine link active เฉพาะที่ `/machine`)

---

### 3.4 Accessibility Attributes

#### `[attr.aria-label]="item.label"`
- **ประเภท**: Attribute binding
- **ทำหน้าที่**: บอก screen readers (สำหรับผู้ใช้ที่มีสายตาบกพร่อง) ว่า link นี้คือ "Home" หรือ "Machine" เป็นต้น
- **ตัวอย่าง**: 
  - `<a aria-label="Home">` → screen reader อ่านว่า "Home" แม้ว่า icon มี emoji เท่านั้น
  - ถ้า sidebar collapse, label ซ่อน (`.hidden-label`) แต่ `aria-label` ยังอ่านได้

#### `[title]="sidebarCollapsed() ? item.label : null"`
- **ประเภท**: Property binding ของ native HTML attribute `title`
- **ทำหน้าที่**: tooltip — เมื่อ hover นาน ๆ จะแสดง tooltip ด้วยข้อความ
- **ตรรมชาติ**:
  - `sidebarCollapsed()` = `true` (sidebar ปิด) → `title="Home"` → เมื่อ hover เห็น tooltip "Home"
  - `sidebarCollapsed()` = `false` (sidebar เปิด) → `title=null` (ไม่ต้อง tooltip เพราะ label visible อยู่)

#### `<ng-icon class="nav-icon icon-nav" [name]="item.icon" aria-hidden="true" />`
- **`aria-hidden="true"`**: บอก screen reader ให้ละเว้น element นี้
  - **เหตุผล**: icon เป็น visual decoration (SVG จาก ng-icons) ไม่มีความหมายข้อความ ถ้า screen reader อ่านออกมาจะ confusing
  - `aria-label` ของ `<a>` tag ก็พอ
- **`[name]="item.icon"`**: อ้างชื่อ icon ที่ลงทะเบียนใน `provideIcons()` — ใช้ค่าจาก `APP_ICONS` ใน `shared/app-icons.ts`

#### `<span class="nav-label" [class.hidden-label]="sidebarCollapsed()">{{ item.label }}</span>`
- **Class binding**: `[class.hidden-label]="sidebarCollapsed()`
  - ถ้า `sidebarCollapsed()` = `true` → เพิ่ม class `hidden-label` → label ซ่อน (CSS: `display: none` หรือ `visibility: hidden`)
  - ถ้า `sidebarCollapsed()` = `false` → ไม่มี class `hidden-label` → label visible
- **ข้อความ**: `{{ item.label }}` — แสดงชื่อ menu item (เช่น "Home", "Machine")

---

### 3.5 Flow: User Interaction

```
1. Page Load
   → app-shell.component.ts สร้าง navItems array ที่มี route, label, icon
   → @for loop render <a> tag สำหรับแต่ละ item

2. Router State
   → Angular Router ติดตามว่า current route = ?
   → routerLinkActive directive ตรวจ: link route === current route?
   → ถ้าตรง → เพิ่ม class="active" ให้กับ <a> tag นั้น
   → CSS render background/color ของ active link

3. User Click on Link
   → <a routerLink="machine"> click
   → Angular Router navigate ไป /machine
   → AppShellComponent child <router-outlet /> render MachineComponent
   → routerLinkActive ตรวจ: link route "machine" === current route "/machine"? ✓
   → routerLinkActive เพิ่ม class="active" ให้ link นี้

4. Sidebar Collapse Toggle
   → User click toggle button
   → component.sidebarCollapsed() บันทึก state ใหม่ (toggle boolean)
   → [class.hidden-label]="sidebarCollapsed()" → ตัดสินใจ ซ่อนหรือแสดง label
   → [title] ปรับ: ถ้าปิด → show tooltip, เปิด → remove tooltip
```

---

### 3.6 Data Structure: `navItems` Array

ตัวอย่างโครงสร้าง component ที่เสริม:

```typescript
// app-shell.component.ts
import { APP_ICONS, AppIconName } from '../shared/app-icons';

interface NavItem {
    label: string;
    icon: AppIconName;
    route: string;
}

export class AppShellComponent {
    navItems: NavItem[] = [
        { route: '/', label: 'Home', icon: APP_ICONS.navHome },
        { route: 'machine', label: 'Machine', icon: APP_ICONS.navMachine },
    ];
    
    sidebarCollapsed = signal(false);
    currentUser = signal<User | null>(null);
    isAdmin = computed(() => this.currentUser()?.role === 'admin');
    
    toggleSidebar() {
        this.sidebarCollapsed.update(v => !v);
        localStorage.setItem('app-sidebar-collapsed', this.sidebarCollapsed().toString());
    }
    
    logout() {
        this.authService.logout();
        this.router.navigate(['/login']);
    }
}
```

---

### 3.7 CSS Connection (Example)

ใน `app-shell.component.css`:

```css
.sidebar-nav {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.nav-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    border-radius: 0.375rem;
    color: inherit;
    text-decoration: none;
    transition: background-color 0.2s;
}

.nav-item:hover {
    background-color: rgba(255, 255, 255, 0.1);
}

/* Active state — routerLinkActive="active" เพิ่ม class นี้ */
.nav-item.active {
    background-color: rgba(59, 130, 246, 0.2);
    color: rgb(59, 130, 246);
    font-weight: 600;
}

.nav-icon {
    flex-shrink: 0;
}

.nav-label {
    flex: 1;
}

/* Sidebar collapse — app-shell [class.sidebar-collapsed]="sidebarCollapsed()" */
.app-shell.sidebar-collapsed .nav-label {
    display: none;
}

.app-shell.sidebar-collapsed .nav-item {
    justify-content: center;
    padding: 0.75rem;
}

.hidden-label {
    display: none;
}
```

---

### 3.8 Checklist: ตรวจ Navigation Implementation

- [ ] `navItems` array มี properties: `route`, `label`, `icon` (type `AppIconName` จาก `APP_ICONS`)
- [ ] `@for (item of navItems; track item.route)` ใช้ `track` ไม่ลืม
- [ ] `[routerLink]="item.route"` ผูก route ใน item กับ Angular Router
- [ ] `routerLinkActive="active"` highlight active link ด้วย CSS class
- [ ] `[routerLinkActiveOptions]="{ exact: item.route === '/' }"` ใช้ exact match เฉพาะ home
- [ ] `[attr.aria-label]="item.label"` ให้ screen reader อ่านได้ (accessibility)
- [ ] `aria-hidden="true"` ใน icon span เพื่อไม่ให้ screen reader อ่าน
- [ ] `[title]` tooltip แสดงเฉพาะตอน sidebar ปิด
- [ ] `sidebarCollapsed()` signal ควบคุมซ่อน/แสดง label
- [ ] `.nav-item.active` CSS มีสีหรือ background ที่ชัดเจน
- [ ] เพิ่มเมนูใหม่ → แก้ `navItems` ใน `.ts` ไฟล์เท่านั้น (ไม่ต้องแก้ HTML)
- [ ] Route ใหม่ → เพิ่ม `children` ใน `app.routes.ts` และ nav item ใน `navItems`

---

### 3.9 ตัวอย่าง: เพิ่มเมนู "Reports"

**ขั้นตอน**:

1. **อัปเดต `app.routes.ts`**:
   ```typescript
   children: [
       { path: '', component: HomeComponent },
       { path: 'machine', component: MachineComponent },
       { path: 'reports', component: ReportsComponent },  // ← เพิ่ม
   ],
   ```

2. **อัปเดต `navItems` ใน `app-shell.component.ts`**:
   ```typescript
   navItems = [
       { route: '/', label: 'Home', icon: APP_ICONS.navHome },
       { route: 'machine', label: 'Machine', icon: APP_ICONS.navMachine },
       { route: 'reports', label: 'Reports', icon: 'heroChartBar' },  // ← เพิ่ม + ลงทะเบียนใน app-icons.ts
   ];
   ```

3. **ก็เสร็จแล้ว** — HTML `@for` loop จะ auto-render link ใหม่ทั้ง routing + nav display

---

### 3.10 Advanced: Conditional Menu Items (Role-based)

ถ้าต้องการ menu ต่างกันตาม role (เช่น admin เท่านั้นเห็น "Settings"):

```typescript
// app-shell.component.ts
navItems = computed(() => {
    const base: NavItem[] = [
        { route: '/', label: 'Home', icon: APP_ICONS.navHome },
        { route: 'machine', label: 'Machine', icon: APP_ICONS.navMachine },
    ];
    
    // ถ้า admin ให้แสดง settings
    if (this.isAdmin()) {
        base.push({ route: 'settings', label: 'Settings', icon: APP_ICONS.navMachine });
    }
    
    return base;
});
```

## 4. Tailwind + Icon Conventions

### 4.1 Tailwind setup

- Dependencies: `tailwindcss`, `@tailwindcss/postcss` (dev) — ติดตั้งผ่าน Docker/build (`npm install` ใน Dockerfile)
- Config: `frontend/postcss.config.json`
- Entry: `@import 'tailwindcss'` ใน `frontend/src/styles.css`
- Shared UI patterns อยู่ใน `@layer components` ของ `styles.css` (`.btn`, `.form-input`, `.alert`, spinners)
- Template ใหม่ควรใช้ Tailwind utility classes สำหรับ layout/spacing ก่อนสร้าง component CSS ใหม่

### 4.2 Icon system (@ng-icons)

- Packages: `@ng-icons/core`, `@ng-icons/heroicons`, `@ng-icons/lucide`, `@ng-icons/huge-icons`
- ลงทะเบียนครั้งเดียวใน `app.config.ts`: `provideIcons(APP_ICON_REGISTRY)`
- Semantic map: `frontend/src/app/shared/app-icons.ts` — ใช้ `APP_ICONS` แทน hardcode ชื่อ icon ใน template
- Template pattern:
  ```html
  <ng-icon class="icon-inline" [name]="icons.close" aria-hidden="true" />
  ```
- Component ที่ใช้ `<ng-icon>` ต้อง `imports: [NgIcon]` ใน standalone component
- **ห้ามใช้ emoji** เป็น icon ใน UI — ใช้ ng-icons จาก pack ที่เหมาะสม:
  - Navigation / chrome → heroicons
  - Action buttons (edit/delete) → lucide
  - Search / logout → huge-icons

---

*เอกสารนี้อ้างอิงจากโครงสร้างปัจจุบันของ Basic App (Angular standalone + AppShell + Tailwind + ng-icons)*
