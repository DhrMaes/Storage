# Storage Explorer Web App - Setup Guide

A visual file explorer web application for the Storage Cluster, built with Angular and ASP.NET Core Web API.

---

## Architecture

```
┌─────────────────────────────────────┐
│   Angular Frontend                  │
│   - File Explorer UI                │
│   - Drag & Drop Upload              │
│   - Context Menus                   │
│   - Multiple Views (List/Grid)      │
└────────────┬────────────────────────┘
             │ HTTP/REST API
             ▼
┌─────────────────────────────────────┐
│   ASP.NET Core Web API              │
│   - Storage Controller              │
│   - Providers Controller            │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│   StorageOrchestrator               │
└─────────────────────────────────────┘
```

---

## Backend Setup (Already Complete!)

### ✅ ASP.NET Core Web API

**Project:** `Storage.WebApi`

**Controllers:**
1. **StorageController** (`/api/storage`)
   - `GET /list?path=/` - List directory contents
   - `GET /info?path=/file.txt` - Get file/directory info
   - `GET /download?path=/file.txt` - Download file
   - `POST /upload?path=/` - Upload file
   - `DELETE /delete?path=/file.txt` - Delete file/directory
   - `POST /move` - Move/rename file
   - `GET /exists?path=/file.txt` - Check if path exists

2. **ProvidersController** (`/api/providers`)
   - `GET /` - List all configured providers
   - `GET /default` - Get default provider name

**Features:**
- ✅ CORS enabled for Angular (http://localhost:4200)
- ✅ Swagger/OpenAPI documentation
- ✅ JSON serialization
- ✅ Error handling with proper HTTP status codes
- ✅ File upload/download support
- ✅ Logging

**Configuration:**
- Create `storage-config.json` in the Web API project directory
- Example provided: `Storage.WebApi\storage-config.json`

**Running the API:**
```bash
cd Storage.WebApi
dotnet run
```

API will be available at: `https://localhost:5001` or `http://localhost:5000`

Swagger UI: `https://localhost:5001/swagger`

---

## Frontend Setup (Next Steps)

### 1. Install Angular CLI

```bash
npm install -g @angular/cli
```

### 2. Create Angular Project

```bash
# Navigate to solution root
cd C:\Users\ArneMA\Repos\Personal\Storage

# Create Angular app
ng new storage-web-ui --routing --style=scss
cd storage-web-ui
```

### 3. Install Dependencies

```bash
# Angular Material for UI components
ng add @angular/material

# HTTP client (already included in Angular)
# File upload library
npm install ngx-file-drop

# Icons
npm install @angular/material-icons
```

### 4. Generate Components

```bash
# Main file explorer component
ng generate component components/file-explorer

# Breadcrumb navigation
ng generate component components/breadcrumb

# File list view
ng generate component components/file-list

# File grid view  
ng generate component components/file-grid

# Upload dialog
ng generate component dialogs/upload-dialog

# Delete confirmation
ng generate component dialogs/confirm-dialog

# Rename dialog
ng generate component dialogs/rename-dialog

# Services
ng generate service services/storage
ng generate service services/file-icon
```

### 5. Project Structure

```
storage-web-ui/
├── src/
│   ├── app/
│   │   ├── components/
│   │   │   ├── file-explorer/      # Main container
│   │   │   ├── breadcrumb/         # Path navigation
│   │   │   ├── file-list/          # List view
│   │   │   └── file-grid/          # Grid view
│   │   ├── dialogs/
│   │   │   ├── upload-dialog/      # File upload
│   │   │   ├── confirm-dialog/     # Delete confirmation
│   │   │   └── rename-dialog/      # Rename/move
│   │   ├── services/
│   │   │   ├── storage.service.ts  # API calls
│   │   │   └── file-icon.service.ts # File type icons
│   │   ├── models/
│   │   │   ├── storage-item.ts     # File/folder model
│   │   │   └── provider.ts         # Provider model
│   │   └── app.component.ts
│   └── environments/
│       ├── environment.ts          # Dev: API at localhost:5000
│       └── environment.prod.ts     # Prod: API at production URL
```

---

## Planned Features

### Phase 1 - Basic Explorer ✅ (Backend Ready)
- [x] List files and directories
- [x] Navigate folder tree
- [x] Download files
- [x] Upload files
- [x] Delete files/folders
- [x] Rename/move files

### Phase 2 - Enhanced UI (Frontend)
- [ ] Breadcrumb navigation
- [ ] List view with sortable columns
- [ ] Grid view with thumbnails
- [ ] File type icons
- [ ] Context menu (right-click)
- [ ] Keyboard shortcuts
- [ ] Multiple selection

### Phase 3 - Advanced Features
- [ ] Drag & drop upload
- [ ] Drag & drop move
- [ ] Search files
- [ ] File preview (images, text, PDFs)
- [ ] Copy/paste files
- [ ] Zip download multiple files
- [ ] Progress indicators for uploads/downloads

### Phase 4 - Multi-Provider
- [ ] Provider selector dropdown
- [ ] Switch between providers
- [ ] Copy files between providers
- [ ] Provider status indicators

### Phase 5 - Admin Features
- [ ] Provider management UI
- [ ] Plugin management UI
- [ ] Storage statistics dashboard
- [ ] Activity logs

---

## Key Angular Components to Build

### 1. FileExplorerComponent (Main Container)
```typescript
@Component({
  selector: 'app-file-explorer',
  template: `
    <mat-toolbar color="primary">
      <span>Storage Explorer</span>
      <span class="spacer"></span>
      <button mat-icon-button (click)="uploadFile()">
        <mat-icon>cloud_upload</mat-icon>
      </button>
      <button mat-icon-button (click)="toggleView()">
        <mat-icon>{{viewMode === 'list' ? 'grid_view' : 'list'}}</mat-icon>
      </button>
    </mat-toolbar>
    
    <app-breadcrumb [path]="currentPath" (navigate)="navigateTo($event)"></app-breadcrumb>
    
    <app-file-list *ngIf="viewMode === 'list'"
      [items]="items"
      [loading]="loading"
      (itemClick)="onItemClick($event)"
      (itemDelete)="onItemDelete($event)"
      (itemRename)="onItemRename($event)"
      (itemDownload)="onItemDownload($event)">
    </app-file-list>
    
    <app-file-grid *ngIf="viewMode === 'grid'"
      [items]="items"
      [loading]="loading"
      (itemClick)="onItemClick($event)">
    </app-file-grid>
  `
})
export class FileExplorerComponent {
  currentPath = '/';
  items: StorageItem[] = [];
  loading = false;
  viewMode: 'list' | 'grid' = 'list';
}
```

### 2. StorageService (API Client)
```typescript
@Injectable({ providedIn: 'root' })
export class StorageService {
  private apiUrl = 'https://localhost:5001/api/storage';
  
  constructor(private http: HttpClient) {}
  
  list(path: string): Observable<StorageItem[]> {
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
}
```

### 3. Models
```typescript
// storage-item.ts
export interface StorageItem {
  path: string;
  itemType: 'File' | 'Directory';
  size?: number;
  lastModified: string;
  metadata?: { [key: string]: string };
}

// provider.ts
export interface Provider {
  name: string;
  pluginId: string;
  settings: { [key: string]: string };
  isDefault: boolean;
}
```

---

## UI Design Inspiration

**Similar to:**
- Windows File Explorer
- Google Drive web interface
- Dropbox web interface
- VS Code file tree

**Features to Include:**
- Clean, modern Material Design
- Responsive layout
- Dark mode support
- Keyboard navigation
- Accessibility (ARIA labels)

---

## Development Workflow

1. **Start Backend:**
   ```bash
   cd Storage.WebApi
   dotnet watch run
   ```

2. **Start Frontend:**
   ```bash
   cd storage-web-ui
   ng serve
   ```

3. **Access:**
   - Frontend: http://localhost:4200
   - Backend: https://localhost:5001
   - Swagger: https://localhost:5001/swagger

---

## Next Steps

1. Create Angular project with instructions above
2. Implement basic file explorer component
3. Add Material Design UI
4. Implement file operations
5. Add drag & drop support
6. Enhance with thumbnails/previews
7. Add provider management UI

Ready to start building the Angular frontend! 🚀
