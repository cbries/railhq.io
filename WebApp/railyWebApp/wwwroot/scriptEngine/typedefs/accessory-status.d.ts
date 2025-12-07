/**
 * Repräsentiert den Status eines Zubehörs (z. B. Weiche, Signal).
 */
interface AccessoryStatus {
    driverName: string;
    address: number;
    name: string;
    type: string;
    currentState: string;
}
