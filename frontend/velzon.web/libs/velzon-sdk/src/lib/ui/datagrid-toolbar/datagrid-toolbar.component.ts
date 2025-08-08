import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'sdk-datagrid-toolbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './datagrid-toolbar.component.html',
  styleUrl: './datagrid-toolbar.component.scss',
})
export class DatagridToolbarComponent {

    @Output() onGlobalFilterChanged =  new EventEmitter<any>();
    /**Evento disparado quando o botão novo registro é clicado */
    @Output() onNewRegistry =  new EventEmitter<any>();
    @Output() onRefreshData =  new EventEmitter<any>();
    @Output() onToggleFilterRow =  new EventEmitter<any>();
    @Output() onTogglePanelGroup =  new EventEmitter<any>();
    @Output() onExportToFile =  new EventEmitter<any>();
    
    panelGroupvisible: boolean = false;
    filterRowVisible: boolean = false;

    toggleGroupToolTip: string  = "Exibir painel de grupo";
    toggleRowFilterToolTip: string = "Exibir linha de filtro";

    inserirNovo(event: any)
    {
       this.onNewRegistry.emit(event);
    }

    globalFilterChanged(event: any): void
    {
       this.onGlobalFilterChanged.emit(event);
    }

    refreshDataClicked(event: any) : void
  {
    this.onRefreshData.emit(event);
  }

  toggleFilterRowClicked(event: any): void
  {
     this.onToggleFilterRow.emit(event);

     this.filterRowVisible = !this.filterRowVisible;

     this.toggleRowFilterToolTip = this.filterRowVisible ? "Exibir linha de filtro" : "Ocultar linha de filtro";
  }

  togglePanelGroupClicked(event: any): void
  {
    this.onTogglePanelGroup.emit(event);

    this.panelGroupvisible = !this.panelGroupvisible;

    this.toggleGroupToolTip = this.panelGroupvisible ? "Ocultar painel de grupo" : "Exibir painel de grupo";

  }

  exportToFile(event: any)
  {
     this.onExportToFile.emit(event);
  }



}
