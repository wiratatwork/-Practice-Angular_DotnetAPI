export interface Machine {
  machineNo: string;
  machineName: string;
  plant: string;
  status: string;
}

export interface CreateMachineDto {
  machineNo: string;
  machineName: string;
  plant: string;
  status: string;
}

export interface UpdateMachineDto {
  machineName: string;
  plant: string;
  status: string;
}
