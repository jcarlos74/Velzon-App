import { instanceToPlain } from 'class-transformer';

export abstract class ModelBase
{

    toObject(): object
    {
        let obj: any = instanceToPlain(this);
        return obj;
    }
}
