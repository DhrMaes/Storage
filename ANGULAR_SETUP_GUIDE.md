# Angular Frontend Setup - Complete Beginner's Guide

This guide will walk you through setting up the Angular frontend for the Storage Explorer, even if you've never used Angular before.

---

## Prerequisites Installation

### 1. Install Node.js

**What is Node.js?**
Node.js is a JavaScript runtime that Angular needs to run. It includes `npm` (Node Package Manager) which we use to install Angular and other dependencies.

**Steps:**
1. Go to: https://nodejs.org/
2. Download the **LTS (Long Term Support)** version (currently v22.x or similar)
3. Run the installer
4. Accept all default options
5. Restart your terminal/PowerShell after installation

**Verify Installation:**
```powershell
node --version
# Should show: v22.x.x (or similar)

npm --version
# Should show: 10.x.x (or similar)
```

### 2. Install Angular CLI

**What is Angular CLI?**
Angular CLI (Command Line Interface) is a tool that helps you create and manage Angular projects.

**Install:**
```powershell
npm install -g @angular/cli
```

**Verify:**
```powershell
ng version
# Should show Angular CLI version
```

---

## Create the Angular Project

### 1. Navigate to Your Solution Directory

```powershell
cd C:\Users\ArneMA\Repos\Personal\Storage
```

### 2. Create Angular Application

```powershell
ng new storage-web-ui --routing --style=scss --skip-git
```

**What do these options mean?**
- `storage-web-ui` - Project name
- `--routing` - Sets up routing (navigation between pages)
- `--style=scss` - Use SCSS for styling (more powerful than CSS)
- `--skip-git` - Don't create a new Git repo (we're already in one)

**During creation, it will ask:**
- **"Would you like to add server-side rendering (SSR)?"** → Answer: **No**

This will take a few minutes to create the project and install dependencies.

### 3. Navigate into Project

```powershell
cd storage-web-ui
```

### 4. Install Additional Dependencies

```powershell
# Angular Material (UI components like buttons, menus, dialogs)
ng add @angular/material

# During installation, answer:
# - Choose a prebuilt theme: Indigo/Pink (or your preference)
# - Set up global typography styles: Yes
# - Include browser animations: Yes

# File upload library
npm install ngx-file-drop

# HTTP client is already included in Angular
```

---

## Project Structure Overview

After creation, your project will look like this:

```
storage-web-ui/
├── src/
│   ├── app/                    # Your application code
│   │   ├── app.component.ts    # Root component
│   │   ├── app.component.html  # Root template
│   │   ├── app.component.scss  # Root styles
│   │   └── app.routes.ts       # Routing configuration
│   ├── index.html              # Main HTML file
│   ├── main.ts                 # Entry point
│   └── styles.scss             # Global styles
├── angular.json                # Angular configuration
├── package.json                # Dependencies list
└── tsconfig.json               # TypeScript configuration
```

**Key Concepts:**
- **Components** - Reusable UI pieces (like a file list, breadcrumb, etc.)
- **Services** - Business logic and API calls
- **Modules** - Group related components together
- **Routing** - Navigation between different views

---

## Generate Initial Components and Services

Run these commands to create all the pieces we need:

```powershell
# Main components
ng generate component components/file-explorer
ng generate component components/breadcrumb
ng generate component components/file-list
ng generate component components/file-grid
ng generate component components/toolbar

# Dialogs (popup windows)
ng generate component dialogs/upload-dialog
ng generate component dialogs/confirm-dialog
ng generate component dialogs/rename-dialog

# Services (API communication)
ng generate service services/storage
ng generate service services/file-icon

# Models (TypeScript interfaces)
ng generate interface models/storage-item
ng generate interface models/provider
```

**What each command does:**
- Creates a `.ts` file (TypeScript code)
- Creates a `.html` file (template/UI)
- Creates a `.scss` file (styles)
- Creates a `.spec.ts` file (tests - can ignore for now)

---

## Configure API URL

### 1. Update Environment Configuration

Edit `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'  // Your Web API URL
};
```

Create `src/environments/environment.development.ts` (if it doesn't exist):

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};
```

---

## Basic File Structure We'll Create

### 1. Models (Data Structures)

**`src/app/models/storage-item.ts`:**
```typescript
export interface StorageItem {
  path: string;
  itemType: 'File' | 'Directory';
  size?: number;
  lastModified: string;
  metadata?: { [key: string]: string };
}
```

**`src/app/models/provider.ts`:**
```typescript
export interface Provider {
  name: string;
  pluginId: string;
  settings: { [key: string]: string };
  isDefault: boolean;
}
```

### 2. Storage Service (API Client)

**`src/app/services/storage.service.ts`:**
```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StorageItem } from '../models/storage-item';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private apiUrl = `${environment.apiUrl}/storage`;

  constructor(private http: HttpClient) {}

  list(path: string = '/'): Observable<StorageItem[]> {
    return this.http.get<StorageItem[]>(`${this.apiUrl}/list`, {
      params: { path }
    });
  }

  download(path: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download`, {
      params: { path },
      responseType: 'blob'
    });
  }

  upload(path: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/upload`, formData, {
      params: { path }
    });
  }

  delete(path: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/delete`, {
      params: { path }
    });
  }

  move(sourcePath: string, destinationPath: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/move`, {
      sourcePath,
      destinationPath
    });
  }

  exists(path: string): Observable<{ path: string; exists: boolean }> {
    return this.http.get<{ path: string; exists: boolean }>(
      `${this.apiUrl}/exists`,
      { params: { path } }
    );
  }

  getInfo(path: string): Observable<StorageItem> {
    return this.http.get<StorageItem>(`${this.apiUrl}/info`, {
      params: { path }
    });
  }
}
```

### 3. Main App Component

**`src/app/app.component.ts`:**
```typescript
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FileExplorerComponent } from './components/file-explorer/file-explorer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, FileExplorerComponent],
  template: '<app-file-explorer></app-file-explorer>',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'Storage Explorer';
}
```

---

## Running the Application

### 1. Start the Backend (Web API)

In one terminal:
```powershell
cd C:\Users\ArneMA\Repos\Personal\Storage\Storage.WebApi
dotnet run
```

Backend will run at: `https://localhost:5001`

### 2. Start the Frontend (Angular)

In another terminal:
```powershell
cd C:\Users\ArneMA\Repos\Personal\Storage\storage-web-ui
ng serve
```

Frontend will run at: `http://localhost:4200`

**Open your browser:** http://localhost:4200

---

## Development Workflow

### Making Changes

Angular has **hot reload** - any changes you save will automatically refresh in the browser!

1. Edit files in your IDE (Visual Studio, VS Code, etc.)
2. Save the file
3. Browser automatically refreshes with changes

### Common Commands

```powershell
# Start development server
ng serve

# Start with custom port
ng serve --port 4300

# Open browser automatically
ng serve --open

# Build for production
ng build

# Run tests
ng test

# Generate new component
ng generate component components/my-new-component

# Generate new service
ng generate service services/my-service
```

---

## Angular Basics Quick Reference

### Component Structure

```typescript
@Component({
  selector: 'app-my-component',      // HTML tag: <app-my-component>
  templateUrl: './my.component.html', // HTML template
  styleUrls: ['./my.component.scss']  // CSS styles
})
export class MyComponent {
  // Properties (data)
  title = 'Hello';
  items: string[] = [];
  
  // Methods (functions)
  onClick() {
    console.log('Clicked!');
  }
  
  // Lifecycle hooks
  ngOnInit() {
    // Runs when component initializes
  }
}
```

### Template Syntax

```html
<!-- Display data -->
<h1>{{ title }}</h1>

<!-- Loop through array -->
<div *ngFor="let item of items">
  {{ item }}
</div>

<!-- Conditional display -->
<div *ngIf="loading">Loading...</div>

<!-- Event binding -->
<button (click)="onClick()">Click me</button>

<!-- Two-way binding -->
<input [(ngModel)]="title">

<!-- Pass data to child component -->
<app-child [data]="myData"></app-child>

<!-- Listen to child events -->
<app-child (eventName)="handleEvent($event)"></app-child>
```

### Service Example

```typescript
@Injectable({ providedIn: 'root' })
export class MyService {
  constructor(private http: HttpClient) {}
  
  getData(): Observable<any[]> {
    return this.http.get<any[]>('https://api.example.com/data');
  }
}
```

### Using Service in Component

```typescript
export class MyComponent {
  data: any[] = [];
  
  constructor(private myService: MyService) {}
  
  ngOnInit() {
    this.myService.getData().subscribe({
      next: (result) => {
        this.data = result;
      },
      error: (err) => {
        console.error('Error:', err);
      }
    });
  }
}
```

---

## Next Steps

Once you have the basic setup:

1. **Test the API connection** - Create a simple component that calls `storageService.list('/')`
2. **Build the file list** - Display files in a table
3. **Add navigation** - Click folders to navigate
4. **Add upload** - Drag & drop or button to upload files
5. **Add actions** - Download, delete, rename buttons

I'll help you build each piece step-by-step! 🚀

---

## Troubleshooting

### "npm is not recognized"
- Node.js not installed or not in PATH
- Restart terminal after installing Node.js

### "ng is not recognized"
- Angular CLI not installed: `npm install -g @angular/cli`
- Restart terminal after installing

### CORS errors in browser
- Make sure Web API is running
- Check CORS configuration in `Program.cs`
- Verify API URL in Angular environment config

### Port already in use
- Angular: `ng serve --port 4300`
- Stop other instances or use different port

---

## Useful Resources

- **Angular Docs:** https://angular.dev/
- **Angular Material:** https://material.angular.io/
- **TypeScript Docs:** https://www.typescriptlang.org/docs/
- **RxJS (Observables):** https://rxjs.dev/

---

Ready to start! Once Node.js is installed, we'll create the project together. 🎨
