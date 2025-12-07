declare interface LocomotiveStatus {
        driverName: string;
        address: number;
        name?: string;
        decoderType?: string;
        isDriving: boolean;
        isFunctionAvailable: boolean;
        speed: number;
        speedSteps: number;
        direction: number;
        functions: Record<number, boolean>;
        blockId?: string;
        automationState?: string;
        tags?: string[];
    }