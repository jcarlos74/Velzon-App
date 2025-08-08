import { Injectable } from "@angular/core";
import { DataStateChangeEventArgs, Sorts } from "@syncfusion/ej2-angular-grids";
import { map, Observable, Subject } from "rxjs";
import { GrupoAcessoService } from "./grupo-acesso.http-service";

@Injectable({
    providedIn: 'root',
})
export class GridService extends Subject<DataStateChangeEventArgs> {
    
    BASE_URL: 'api/velzon/teste/';
    urlApiService :string;

    constructor() {

        super();

    }

    public setEndPoint(endpoint: string) {
       this.urlApiService = `${this.BASE_URL}/${endpoint}`;
    }
    public execute(state: any): void {
        this.getData(state).subscribe((x) => super.next(x));
    }

    protected getData(
        state: DataStateChangeEventArgs
    ): Observable<DataStateChangeEventArgs> {
        const pageQuery = `$skip=${state.skip}&$top=${state.take}`;
        let sortQuery: string = '';

        if ((state.sorted || []).length) {
            sortQuery =
                `&$orderby=` +
                state.sorted
                    .map((obj: Sorts) => {
                        return obj.direction === 'descending'
                            ? `${obj.name} desc`
                            : obj.name;
                    })
                    .reverse()
                    .join(',');
        }

        return this.fetchData(
          //  `${this.BASE_URL}?${pageQuery}${sortQuery}&$count=true`
          this.urlApiService
        ).pipe(
            map((response: any) => {
                const result = response['value'];
                const count = response['@odata.count'];
                return { result, count } as DataStateChangeEventArgs;
            })
        );
    }

    private fetchData(url: string): Observable<any> {
        return new Observable((observer) => {
            fetch(url)
                .then((response) => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then((data) => {
                    observer.next(data);
                    observer.complete();
                })
                .catch((error) => {
                    observer.error(error);
                });
        });
    }
}
