import 'reflect-metadata';

export interface FormFieldOptions {
    label: string;
    type: 'text' | 'number' | 'checkbox' | 'dropdown' | 'date' | 'autocomplete';
    required?: boolean;
    options?: { label: string; value: any }[];
    minLength?: number;
    maxLength?: number;
    tab?: string; // Nome da aba
  }

  const formMetadataKey = Symbol('formFields');

  export function FormField(options: FormFieldOptions): PropertyDecorator {
     return (target, propertyKey) => {
        const existing = Reflect.getMetadata(formMetadataKey, target.constructor) || {};
        existing[propertyKey as string] = options;
        Reflect.defineMetadata(formMetadataKey, existing, target.constructor);
     };
  }

  export function getFormMetadata<T>(modelClass: new () => T): Record<keyof T, FormFieldOptions> {
     return Reflect.getMetadata(formMetadataKey, modelClass) || {};
  }
