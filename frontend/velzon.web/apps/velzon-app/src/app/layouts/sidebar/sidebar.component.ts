/* eslint-disable @typescript-eslint/no-explicit-any */
import { Component, OnInit, EventEmitter, Output, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

import { MenuItem } from './menu.model';
import { environment } from '@velzon.web/environments/dev';
import { ApplicationHttpClient } from '@velzon.web/core/http-client';
import { OperationResult } from '@velzon.web/core/model';
import { map } from 'rxjs';
import { SimplebarAngularModule } from 'simplebar-angular';
import { NgClass } from '@angular/common';
import { NgbCollapse } from '@ng-bootstrap/ng-bootstrap';
import { HttpClient } from '@angular/common/http';

@Component({
    selector: 'app-sidebar',
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss'],
    standalone: true,
    imports: [RouterLink, SimplebarAngularModule, NgClass, NgbCollapse, TranslateModule]
})
export class SidebarComponent implements OnInit, AfterViewInit
{

    menu: any;
    toggle: any = true;
    menuItems: MenuItem[] = [];
    @ViewChild('sideMenu') sideMenu!: ElementRef;
    @Output() mobileMenuButtonClicked = new EventEmitter();

    //constructor(private http: ApplicationHttpClient, private router: Router, public translate: TranslateService)
    constructor(private http: HttpClient, private router: Router, public translate: TranslateService)
    {
        translate.setDefaultLang('pt-br');
    }

    ngOnInit(): void
    {

        // Menu Items
      /*  let items = this.http.Get<OperationResult<MenuItem[]>>('lista-itens-menu/ADMIN/39')
            .pipe(map(result => { return result.data; }));*/

        // this.menuItems =items.subscribe((data:MenuItem[]) => data);
        
        //  this.menuItems = this.http.Get<OperationResult<MenuItem[]>>('lista-itens-menu/ADMIN/39')
       //this.http.Get<OperationResult<MenuItem[]>>('/api/cta/account/lista-itens-menu/ADMIN/1')
         //   .subscribe((items) => { this.menuItems = items.data; });

      /* hj = this.http.get<OperationResult<MenuItem[]>>('/api/cta/account/lista-itens-menu/ADMIN/1')
                .subscribe((items) => { this.menuItems = items.data; });*/

        this.router.events.subscribe((event) =>
        {
            if (document.documentElement.getAttribute('data-layout') != "twocolumn") {
                if (event instanceof NavigationEnd) {
                   this.initActiveMenu();
                }
            }
        });
    }

    /***
     * Activate droup down set
     */
    ngAfterViewInit()
    {
        setTimeout(() =>
        {
            this.initActiveMenu();
        }, 0);
    }

    removeActivation(items: any)
    {
        items.forEach((item: any) =>
        {
            item.classList.remove("active");
        });
    }

    toggleItem(item: any)
    {
        this.menuItems.forEach((menuItem: any) =>
        {

            if (menuItem == item) {
                menuItem.isCollapsed = !menuItem.isCollapsed
            } else {
                menuItem.isCollapsed = true
            }
            if (menuItem.subItems) {
                menuItem.subItems.forEach((subItem: any) =>
                {

                    if (subItem == item) {
                        menuItem.isCollapsed = !menuItem.isCollapsed
                        subItem.isCollapsed = !subItem.isCollapsed
                    } else {
                        subItem.isCollapsed = true
                    }
                    if (subItem.subItems) {
                        subItem.subItems.forEach((childitem: any) =>
                        {

                            if (childitem == item) {
                                childitem.isCollapsed = !childitem.isCollapsed
                                subItem.isCollapsed = !subItem.isCollapsed
                                menuItem.isCollapsed = !menuItem.isCollapsed
                            } else {
                                childitem.isCollapsed = true
                            }
                            if (childitem.subItems) {
                                childitem.subItems.forEach((childrenitem: any) =>
                                {

                                    if (childrenitem == item) {
                                        childrenitem.isCollapsed = false
                                        childitem.isCollapsed = false
                                        subItem.isCollapsed = false
                                        menuItem.isCollapsed = false
                                    } else {
                                        childrenitem.isCollapsed = true
                                    }
                                })
                            }
                        })
                    }
                })
            }
        });
    }

    activateParentDropdown(item: any)
    {
        item.classList.add("active");
        const parentCollapseDiv = item.closest(".collapse.menu-dropdown");

        if (parentCollapseDiv) {
            // to set aria expand true remaining
            parentCollapseDiv.parentElement.children[0].classList.add("active");
            if (parentCollapseDiv.parentElement.closest(".collapse.menu-dropdown")) {
                parentCollapseDiv.parentElement.closest(".collapse").classList.add("show");
                if (parentCollapseDiv.parentElement.closest(".collapse").previousElementSibling)
                    parentCollapseDiv.parentElement.closest(".collapse").previousElementSibling.classList.add("active");
                if (parentCollapseDiv.parentElement.closest(".collapse").previousElementSibling.closest(".collapse")) {
                    parentCollapseDiv.parentElement.closest(".collapse").previousElementSibling.closest(".collapse").classList.add("show");
                    parentCollapseDiv.parentElement.closest(".collapse").previousElementSibling.closest(".collapse").previousElementSibling.classList.add("active");
                }
            }
            return false;
        }
        return false;
    }

    updateActive(event: any)
    {
        const ul = document.getElementById("navbar-nav");
        if (ul) {
            const items = Array.from(ul.querySelectorAll("a.nav-link"));
            this.removeActivation(items);
        }
        this.activateParentDropdown(event.target);
    }

    initActiveMenu()
    {
        let pathName = window.location.pathname;
        // Check if the application is running in production
        if (environment.production) {
            // Modify pathName for production build
            pathName = pathName.replace('/velzon/angular/corporate', '');
        }

        const active = this.findMenuItem(pathName, this.menuItems)
        this.toggleItem(active)
        const ul = document.getElementById("navbar-nav");
        if (ul) {
            const items = Array.from(ul.querySelectorAll("a.nav-link"));
            const activeItems = items.filter((x: any) => x.classList.contains("active"));
            this.removeActivation(activeItems);

            const matchingMenuItem = items.find((x: any) =>
            {
                if (environment.production) {
                    let path = x.pathname
                    path = path.replace('/velzon/angular/corporate', '');
                    return path === pathName;
                } else {
                    return x.pathname === pathName;
                }

            });
            if (matchingMenuItem) {
                this.activateParentDropdown(matchingMenuItem);
            }
        }
    }

    private findMenuItem(pathname: string, menuItems: any[]): any
    {
        for (const menuItem of menuItems) {
            if (menuItem.link && menuItem.link === pathname) {
                return menuItem;
            }

            if (menuItem.subItems) {
                const foundItem = this.findMenuItem(pathname, menuItem.subItems);
                if (foundItem) {
                    return foundItem;
                }
            }
        }

        return null;

    }
    /**
     * Returns true or false if given menu item has child or not
     * @param item menuItem
     */
    hasItems(item: MenuItem)
    {
        return item.subItems !== undefined ? item.subItems.length > 0 : false;
    }

    /**
     * Toggle the menu bar when having mobile screen
     */
    toggleMobileMenu(event: any)
    {
        const sidebarsize = document.documentElement.getAttribute("data-sidebar-size");
        if (sidebarsize == 'sm-hover-active') {
            document.documentElement.setAttribute("data-sidebar-size", 'sm-hover')
        } else {
            document.documentElement.setAttribute("data-sidebar-size", 'sm-hover-active')
        }
    }

    /**
     * SidebarHide modal
     * @param content modal content
     */
    SidebarHide()
    {
        document.body.classList.remove('vertical-sidebar-enable');
    }

}
