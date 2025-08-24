import { Component } from 'react';
import {
    Edit24Regular,
    Delete24Regular
} from "@fluentui/react-icons";
import {
    Text, Button,
    DataGridBody,
    DataGridRow,
    DataGrid,
    DataGridHeader,
    DataGridHeaderCell,
    DataGridCell,
    TableCellLayout,
    TableColumnDefinition,
    createTableColumn,
    Tooltip,
    Toolbar,
    ToolbarButton
} from "@fluentui/react-components";
import * as React from 'react';
import "./Home.css"
import { Homework } from "../models/Homework"

// 主列表列
const columns: TableColumnDefinition<Homework>[] = [
    // Content
    createTableColumn<Homework>({
        columnId: "content",
        renderHeaderCell: () => {
            return "内容";
        },
        renderCell: (item) => {
            return (
                <TableCellLayout>
                    {item.Content}
                </TableCellLayout>
            );
        },
    }),
    // Subject
    createTableColumn<Homework>({
        columnId: "subject",
        renderHeaderCell: () => {
            return "科目";
        },
        renderCell: (item) => {
            return (
                <TableCellLayout>
                    {item.Subject}
                </TableCellLayout>
            );
        },
    }),
    // Actions
    createTableColumn<Homework>({
        columnId: "subject",
        renderHeaderCell: () => {
            return "操作";
        },
        renderCell: (item) => {
            return (
                <Toolbar>
                    <Tooltip content="编辑作业" relationship="label">
                        <ToolbarButton icon={<Edit24Regular />}/>
                    </Tooltip>
                    <Tooltip content="删除作业" relationship="label">
                        <ToolbarButton icon={<Delete24Regular />} />
                    </Tooltip>
                </Toolbar>
            );
        },
    }),
]

// 示例数据
const sampleItems: Homework[] = [
    {
        Content: "123",
        Subject: "语文",
        Tags: ["foo"],
        DueTime: ""
    },
    {
        Content: "123",
        Subject: "语文",
        Tags: ["foo"],
        DueTime: ""
    }
]

export class Home extends Component {
  static displayName = Home.name;

    render() {
        return (
            <div className="app-mainview">
                <div className="app-actionbar">
                    <Text className="app-text-header">
                        所有作业
                    </Text>
                    <Button appearance="primary">
                        新建作业
                    </Button>
                </div>
                <div className="app-mainlist-wrapper">
                    <DataGrid columns={columns} items={sampleItems}>
                        <DataGridHeader>
                            <DataGridRow>
                                {({ renderHeaderCell }) => (
                                    <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                                )}
                            </DataGridRow>
                        </DataGridHeader>
                        <DataGridBody<Homework>>
                            {({ item, rowId }) => (
                                <DataGridRow<Homework>
                                    key={rowId}
                                >
                                    {({ renderCell }) => (
                                        <DataGridCell>{renderCell(item)}</DataGridCell>
                                    )}
                                </DataGridRow>
                            )}
                        </DataGridBody>
                    </DataGrid>
                </div>
            </div>
        );
    }
}
