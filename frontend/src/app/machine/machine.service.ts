import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Machine, CreateMachineDto, UpdateMachineDto } from './machine.model';

@Injectable({
    providedIn: 'root',
})
export class MachineService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = '/api/machine';

    getAll(): Observable<Machine[]> {
        return this.http.get<Machine[]>(this.baseUrl);
    }

    getByNo(machineNo: string): Observable<Machine> {
        return this.http.get<Machine>(`${this.baseUrl}/${machineNo}`);
    }

    create(dto: CreateMachineDto): Observable<Machine> {
        return this.http.post<Machine>(this.baseUrl, dto);
    }

    update(machineNo: string, dto: UpdateMachineDto): Observable<Machine> {
        return this.http.patch<Machine>(`${this.baseUrl}/${machineNo}`, dto);
    }

    delete(machineNo: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${machineNo}`);
    }

    search(searchTerm: string): Observable<Machine[]> {
        const encoded = encodeURIComponent(searchTerm.trim());
        return this.http.get<Machine[]>(`${this.baseUrl}/search/${encoded}`);
    }

    checkDuplicateName(
        machineName: string,
        excludeMachineNo?: string
    ): Observable<{ isDuplicate: boolean }> {
        const encodedName = encodeURIComponent(machineName.trim());
        let url = `${this.baseUrl}/checkDuplicateName/${encodedName}`;
        if (excludeMachineNo) {
            url += `?excludeMachineNo=${encodeURIComponent(excludeMachineNo)}`;
        }
        return this.http.get<{ isDuplicate: boolean }>(url);
    }
}
